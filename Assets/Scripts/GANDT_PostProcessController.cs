using UnityEngine;

/// <summary>
/// 셰이더 디버그 출력 모드.
///
/// FINAL      : 최종 합성 화면
/// EDGE_ONLY  : Sobel Edge만 확인
/// BASE_COLOR : Grayscale / Quantize / Dithering이 적용된 기본 화면 확인
/// FLOW_UV    : 기억 오브젝트 방향 UV 변위 확인
/// </summary>
public enum DEBUG_VIEW_MODE
{
    FINAL = 0,
    EDGE_ONLY = 1,
    BASE_COLOR = 2,
    FLOW_UV = 3
}

/// <summary>
/// GANDT 후처리 셰이더 제어용 컨트롤러.
///
/// 역할:
/// - 플레이어/기억 오브젝트/적 상태를 읽어 후처리 Material의 셰이더 파라미터로 전달한다.
/// - Memory Influence, Sanity, Enemy Danger 값을 0~1 범위로 관리한다.
/// - 시연용 Debug UI가 읽을 수 있도록 현재 상태값을 외부에 공개한다.
/// </summary>
public class GANDT_PostProcessController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Memory Target의 월드 좌표를 화면 좌표로 변환할 카메라.")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("Full Screen Pass Renderer Feature에서 사용하는 후처리 머티리얼.")]
    [SerializeField] private Material postProcessMaterial;

    [Tooltip("Memory Influence 거리 계산 기준이 되는 플레이어 Transform.")]
    [SerializeField] private Transform player;

    [Tooltip("기억 오브젝트 Transform. 이 위치가 화면 왜곡의 중심 방향이 된다.")]
    [SerializeField] private Transform memoryTarget;

    [Header("Debug View")]
    [Tooltip("셰이더에서 어떤 중간 결과를 출력할지 선택한다.")]
    [SerializeField] private DEBUG_VIEW_MODE debugViewMode = DEBUG_VIEW_MODE.FINAL;

    [Header("Memory Flow")]
    [Tooltip("플레이어가 기억 오브젝트에 이 거리 이하로 접근하면 Memory Flow가 활성화된다.")]
    [SerializeField] private float memoryDetectRange = 8f;

    [Tooltip("거리 비율을 Memory Influence로 바꾸는 곡선. x=0은 가까움, x=1은 탐지 범위 끝.")]
    [SerializeField]
    private AnimationCurve memoryInfluenceCurve =
        AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Sanity")]
    [Tooltip("플레이어의 이성 수치. 1이면 안정, 0에 가까울수록 에지 흔들림이 강해진다.")]
    [Range(0f, 1f)]
    [SerializeField] private float sanity = 1f;

    [Header("Enemy Danger")]
    [Tooltip("적 접근 위험도. 1에 가까울수록 붉은 에지와 추가 흔들림이 강해진다.")]
    [Range(0f, 1f)]
    [SerializeField] private float enemyDangerInfluence = 0f;

    public float CurrentSanity => sanity;
    public float CurrentMemoryInfluence { get; private set; }
    public float CurrentEnemyDangerInfluence => enemyDangerInfluence;
    public DEBUG_VIEW_MODE CurrentDebugViewMode => debugViewMode;
    public string CurrentDebugViewModeName => debugViewMode.ToString();

    // Shader.PropertyToID를 사용하면 매 프레임 문자열로 프로퍼티를 찾는 비용을 줄일 수 있다.
    private static readonly int MemoryScreenPosID = Shader.PropertyToID("_MemoryScreenPos");
    private static readonly int MemoryInfluenceID = Shader.PropertyToID("_MemoryInfluence");
    private static readonly int SanityID = Shader.PropertyToID("_Sanity");
    private static readonly int UseMemoryFlowID = Shader.PropertyToID("_UseMemoryFlow");
    private static readonly int DebugViewModeID = Shader.PropertyToID("_DebugViewMode");
    private static readonly int EnemyDangerInfluenceID = Shader.PropertyToID("_EnemyDangerInfluence");

    private void Reset()
    {
        targetCamera = Camera.main;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void OnValidate()
    {
        // 셰이더는 0~1 범위를 기준으로 동작하므로 Inspector 값도 항상 제한한다.
        sanity = Mathf.Clamp01(sanity);
        enemyDangerInfluence = Mathf.Clamp01(enemyDangerInfluence);
        memoryDetectRange = Mathf.Max(0.01f, memoryDetectRange);
    }

    private void Update()
    {
        if (targetCamera == null || postProcessMaterial == null)
        {
            return;
        }

        UpdateDebugViewMode();
        UpdateMemoryFlow();
        UpdateSanity();
        UpdateEnemyDanger();
    }

    private void UpdateDebugViewMode()
    {
        // C# enum 값을 float로 변환하여 셰이더의 분기 조건에 전달한다.
        postProcessMaterial.SetFloat(DebugViewModeID, (float)debugViewMode);
    }

    public string GetDebugViewModeName()
    {
        // DebugStatusUI에서 숫자가 아니라 사람이 읽기 쉬운 이름으로 표시하기 위한 함수.
        return debugViewMode switch
        {
            DEBUG_VIEW_MODE.FINAL => "Final",
            DEBUG_VIEW_MODE.EDGE_ONLY => "Edge Only",
            DEBUG_VIEW_MODE.BASE_COLOR => "Base Color",
            DEBUG_VIEW_MODE.FLOW_UV => "Flow UV",
            _ => "Unknown"
        };
    }

    private void UpdateMemoryFlow()
    {
        if (memoryTarget == null || player == null)
        {
            CurrentMemoryInfluence = 0f;

            // 대상이 없을 때는 셰이더에서도 Memory Flow가 절대 남아 있지 않도록 초기화한다.
            postProcessMaterial.SetFloat(UseMemoryFlowID, 0f);
            postProcessMaterial.SetFloat(MemoryInfluenceID, 0f);
            postProcessMaterial.SetVector(MemoryScreenPosID, Vector4.zero);
            return;
        }

        // 월드 좌표의 기억 오브젝트 위치를 화면 UV와 같은 0~1 Viewport 좌표로 변환한다.
        // 이 값이 셰이더에서 UV를 당기는 기준점이 된다.
        Vector3 viewportPos = targetCamera.WorldToViewportPoint(memoryTarget.position);

        bool isInFrontOfCamera = viewportPos.z > 0f;
        bool isOnScreen =
            viewportPos.x >= 0f && viewportPos.x <= 1f &&
            viewportPos.y >= 0f && viewportPos.y <= 1f;

        float distance = Vector3.Distance(player.position, memoryTarget.position);
        float influence = 0f;

        if (isInFrontOfCamera && isOnScreen && distance <= memoryDetectRange)
        {
            float normalizedDistance = Mathf.Clamp01(distance / memoryDetectRange);

            // normalizedDistance가 0이면 매우 가까움, 1이면 탐지 범위 끝.
            // 기본 곡선은 가까울수록 influence가 1에 가까워지도록 설정되어 있다.
            influence = memoryInfluenceCurve.Evaluate(normalizedDistance);
        }

        CurrentMemoryInfluence = influence;

        // _UseMemoryFlow는 셰이더에서 불필요한 UV 변위를 끄는 토글 역할을 한다.
        postProcessMaterial.SetFloat(UseMemoryFlowID, influence > 0.001f ? 1f : 0f);
        postProcessMaterial.SetVector(
            MemoryScreenPosID,
            new Vector4(viewportPos.x, viewportPos.y, 0f, 0f)
        );
        postProcessMaterial.SetFloat(MemoryInfluenceID, influence);
    }

    private void UpdateSanity()
    {
        // Sanity 값은 셰이더에서 1 - Sanity로 불안정성을 계산하는 데 사용된다.
        postProcessMaterial.SetFloat(SanityID, sanity);
    }

    private void UpdateEnemyDanger()
    {
        // Enemy Danger 값은 붉은 에지 색상과 추가 흔들림 강도에 사용된다.
        postProcessMaterial.SetFloat(EnemyDangerInfluenceID, enemyDangerInfluence);
    }

    public void SetDebugViewMode(DEBUG_VIEW_MODE mode)
    {
        debugViewMode = mode;
    }

    public void SetSanity(float value)
    {
        sanity = Mathf.Clamp01(value);
    }

    public void AddSanity(float amount)
    {
        sanity = Mathf.Clamp01(sanity + amount);
    }

    public void SetEnemyDangerInfluence(float value)
    {
        enemyDangerInfluence = Mathf.Clamp01(value);
    }

    public float GetMemoryInfluence()
    {
        return CurrentMemoryInfluence;
    }

    public float GetSanity()
    {
        return sanity;
    }

    public float GetEnemyDangerInfluence()
    {
        return enemyDangerInfluence;
    }
}
