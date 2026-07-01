using UnityEngine;

/// <summary>
/// GANDT 적 오브젝트용 컴포넌트.
///
/// 역할:
/// - 적 중심의 위험 반경 안에 플레이어가 들어왔는지 검사한다.
/// - 가까울수록 Enemy Danger Influence를 1에 가깝게 만든다.
/// - 위험도에 따라 플레이어 Sanity를 감소시키고, 후처리 셰이더의 붉은 에지/흔들림에 값을 전달한다.
/// - Scene View에서는 Gizmo로 감지 반경을 보여주어 시연과 디버깅에 사용한다.
/// </summary>
public class GANDT_EnemyComponent : MonoBehaviour
{
    [Header("References")]
    [Tooltip("위험 반경 판정 기준이 되는 플레이어 Transform.")]
    [SerializeField] private Transform player;

    [Tooltip("Sanity와 Enemy Danger 값을 후처리 셰이더에 전달하는 컨트롤러.")]
    [SerializeField] private GANDT_PostProcessController postProcessController;

    [Header("Danger Zone")]
    [Tooltip("플레이어가 이 반경 안에 들어오면 위험 상태로 판정한다.")]
    [SerializeField] private float dangerRadius = 5f;

    [Tooltip("위험 상태에서 초당 감소할 최대 Sanity 양. 실제 감소량은 거리 기반 위험도에 비례한다.")]
    [SerializeField] private float sanityDrainPerSecond = 0.12f;

    [Header("Visual Debug")]
    [Tooltip("디버그용으로 색을 바꿀 Enemy Renderer.")]
    [SerializeField] private Renderer enemyRenderer;

    [Tooltip("위험하지 않을 때 Enemy 색상.")]
    [SerializeField] private Color normalColor = Color.white;

    [Tooltip("위험할 때 Enemy 색상.")]
    [SerializeField] private Color dangerColor = Color.red;

    [Header("Gizmo")]
    [Tooltip("true이면 Scene View에서 위험 반경을 반투명 구로 함께 표시한다.")]
    [SerializeField] private bool drawSolidGizmo = true;

    [SerializeField] private GANDT_EnemyAI enemyAI;

    public bool IsPlayerInside => isPlayerInside;
    public float CurrentDangerInfluence => currentDangerInfluence;

    private bool isPlayerInside;

    // 0~1 사이의 위험도.
    // 0: 위험 반경 밖 또는 가장자리, 1: 적 중심에 매우 가까움.
    private float currentDangerInfluence;

    // MaterialPropertyBlock을 사용하면 공유 머티리얼을 직접 복사/수정하지 않고 Renderer별 색상만 바꿀 수 있다.
    private MaterialPropertyBlock propertyBlock;

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorID = Shader.PropertyToID("_Color");

    private void Reset()
    {
        enemyRenderer = GetComponentInChildren<Renderer>();

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        postProcessController = FindFirstObjectByType<GANDT_PostProcessController>();
    }

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();

        if (enemyRenderer == null)
        {
            enemyRenderer = GetComponentInChildren<Renderer>();
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (postProcessController == null)
        {
            postProcessController = FindFirstObjectByType<GANDT_PostProcessController>();
        }

        if (enemyAI == null)
        {
            enemyAI = GetComponent<GANDT_EnemyAI>();
        }
    }

    private void OnValidate()
    {
        // Inspector에서 잘못된 값이 들어가도 런타임 판정이 깨지지 않도록 제한한다.
        dangerRadius = Mathf.Max(0.01f, dangerRadius);
        sanityDrainPerSecond = Mathf.Max(0f, sanityDrainPerSecond);
    }

    private void Update()
    {
        if (player == null || postProcessController == null)
        {
            currentDangerInfluence = 0f;
            isPlayerInside = false;
            UpdateEnemyDebugVisual();
            return;
        }

        UpdateDangerState();
        ApplyDangerEffect();
        UpdateEnemyDebugVisual();
    }

    private void UpdateDangerState()
    {
        if (enemyAI != null && enemyAI.CurrentState == ENEMY_STATE.DISABLED)
        {
            isPlayerInside = false;
            currentDangerInfluence = 0f;
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        isPlayerInside = distance <= dangerRadius;

        if (isPlayerInside)
        {
            float normalizedDistance = Mathf.Clamp01(distance / dangerRadius);

            // 가까울수록 1, 위험 반경의 끝에서는 0.
            // 이 값은 후처리 셰이더에서 붉은 에지 색상과 흔들림 강도에 사용된다.
            currentDangerInfluence = 1f - normalizedDistance;
        }
        else
        {
            currentDangerInfluence = 0f;
        }
    }

    private void ApplyDangerEffect()
    {
        if (postProcessController == null)
        {
            return;
        }

        if (isPlayerInside)
        {
            // 가까울수록 currentDangerInfluence가 커지므로 Sanity 감소량도 커진다.
            float drainAmount = sanityDrainPerSecond * currentDangerInfluence * Time.deltaTime;
            postProcessController.AddSanity(-drainAmount);
        }

        // 후처리 셰이더의 _EnemyDangerInfluence로 전달되어 붉은 에지와 추가 흔들림을 만든다.
        postProcessController.SetEnemyDangerInfluence(currentDangerInfluence);
    }

    private void UpdateEnemyDebugVisual()
    {
        if (enemyRenderer == null)
        {
            return;
        }

        Color targetColor = Color.Lerp(normalColor, dangerColor, currentDangerInfluence);

        enemyRenderer.GetPropertyBlock(propertyBlock);

        Material sharedMaterial = enemyRenderer.sharedMaterial;

        // URP Lit 계열은 _BaseColor를, 일부 기본/커스텀 셰이더는 _Color를 사용하므로 둘 다 대응한다.
        if (sharedMaterial != null && sharedMaterial.HasProperty(BaseColorID))
        {
            propertyBlock.SetColor(BaseColorID, targetColor);
        }
        else
        {
            propertyBlock.SetColor(ColorID, targetColor);
        }

        enemyRenderer.SetPropertyBlock(propertyBlock);
    }

    private void OnDrawGizmos()
    {
        Color safeFillColor = new Color(1f, 1f, 0f, 0.15f);
        Color dangerFillColor = new Color(1f, 0f, 0f, 0.22f);

        Color safeWireColor = Color.yellow;
        Color dangerWireColor = Color.red;

        // 위험 반경 안이면 Gizmo도 빨간색으로 바뀌어 시연 시 판정 상태를 확인하기 쉽다.
        Gizmos.color = isPlayerInside ? dangerWireColor : safeWireColor;
        Gizmos.DrawWireSphere(transform.position, dangerRadius);

        if (drawSolidGizmo)
        {
            Gizmos.color = isPlayerInside ? dangerFillColor : safeFillColor;
            Gizmos.DrawSphere(transform.position, dangerRadius);
        }
    }
}
