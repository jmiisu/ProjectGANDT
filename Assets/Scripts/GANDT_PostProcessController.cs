using UnityEngine;

public enum DEBUG_VIEW_MODE
{
    FINAL = 0,
    EDGE_ONLY = 1,
    BASE_COLOR = 2,
    FLOW_UV = 3
}

public class GANDT_PostProcessController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Material postProcessMaterial;
    [SerializeField] private Transform player;
    [SerializeField] private GANDT_PlayerStatus playerStatus;
    [SerializeField] private Transform memoryTarget;

    [Header("Debug View")]
    [SerializeField] private DEBUG_VIEW_MODE debugViewMode = DEBUG_VIEW_MODE.FINAL;

    [Header("Memory Flow")]
    [SerializeField] private float memoryDetectRange = 8f;

    [SerializeField]
    private AnimationCurve memoryInfluenceCurve =
        AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    public float MemoryInfluence { get; private set; }
    public float EnemyDangerInfluence { get; private set; }

    public DEBUG_VIEW_MODE DebugViewMode => debugViewMode;

    private static readonly int MemoryScreenPosID =
        Shader.PropertyToID("_MemoryScreenPos");

    private static readonly int MemoryInfluenceID =
        Shader.PropertyToID("_MemoryInfluence");

    private static readonly int SanityID =
        Shader.PropertyToID("_Sanity");

    private static readonly int UseMemoryFlowID =
        Shader.PropertyToID("_UseMemoryFlow");

    private static readonly int DebugViewModeID =
        Shader.PropertyToID("_DebugViewMode");

    private static readonly int EnemyDangerInfluenceID =
        Shader.PropertyToID("_EnemyDangerInfluence");

    private void Reset()
    {
        targetCamera = Camera.main;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            return;
        }

        player = playerObject.transform;
        playerStatus = playerObject.GetComponent<GANDT_PlayerStatus>();
    }

    private void Awake()
    {
        FindMissingReferences();
    }

    private void OnValidate()
    {
        memoryDetectRange = Mathf.Max(0.01f, memoryDetectRange);
    }

    private void Update()
    {
        if (targetCamera == null || postProcessMaterial == null)
        {
            return;
        }

        UpdateDebugView();
        UpdateMemoryFlow();
        UpdatePlayerStatus();
        UpdateEnemyDanger();
    }

    private void FindMissingReferences()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (player != null)
        {
            if (playerStatus == null)
            {
                playerStatus = player.GetComponent<GANDT_PlayerStatus>();
            }

            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            return;
        }

        player = playerObject.transform;

        if (playerStatus == null)
        {
            playerStatus = playerObject.GetComponent<GANDT_PlayerStatus>();
        }
    }

    private void UpdateDebugView()
    {
        postProcessMaterial.SetFloat(
            DebugViewModeID,
            (float)debugViewMode
        );
    }

    private void UpdateMemoryFlow()
    {
        if (memoryTarget == null || player == null)
        {
            ClearMemoryFlow();
            return;
        }

        Vector3 viewportPosition =
            targetCamera.WorldToViewportPoint(memoryTarget.position);

        bool isInFrontOfCamera = viewportPosition.z > 0f;

        bool isOnScreen =
            viewportPosition.x >= 0f &&
            viewportPosition.x <= 1f &&
            viewportPosition.y >= 0f &&
            viewportPosition.y <= 1f;

        float distance =
            Vector3.Distance(player.position, memoryTarget.position);

        float influence = 0f;

        if (
            isInFrontOfCamera &&
            isOnScreen &&
            distance <= memoryDetectRange
        )
        {
            float normalizedDistance =
                Mathf.Clamp01(distance / memoryDetectRange);

            influence =
                Mathf.Clamp01(
                    memoryInfluenceCurve.Evaluate(normalizedDistance)
                );
        }

        MemoryInfluence = influence;

        postProcessMaterial.SetFloat(
            UseMemoryFlowID,
            influence > 0.001f ? 1f : 0f
        );

        postProcessMaterial.SetVector(
            MemoryScreenPosID,
            new Vector4(
                viewportPosition.x,
                viewportPosition.y,
                0f,
                0f
            )
        );

        postProcessMaterial.SetFloat(
            MemoryInfluenceID,
            MemoryInfluence
        );
    }

    private void ClearMemoryFlow()
    {
        MemoryInfluence = 0f;

        postProcessMaterial.SetFloat(UseMemoryFlowID, 0f);
        postProcessMaterial.SetFloat(MemoryInfluenceID, 0f);
        postProcessMaterial.SetVector(MemoryScreenPosID, Vector4.zero);
    }

    private void UpdatePlayerStatus()
    {
        float sanity =
            playerStatus != null
                ? playerStatus.Sanity
                : 1f;

        postProcessMaterial.SetFloat(SanityID, sanity);
    }

    private void UpdateEnemyDanger()
    {
        postProcessMaterial.SetFloat(
            EnemyDangerInfluenceID,
            EnemyDangerInfluence
        );
    }

    public void SetDebugViewMode(DEBUG_VIEW_MODE mode)
    {
        debugViewMode = mode;
    }

    public void SetMemoryTarget(Transform target)
    {
        memoryTarget = target;
    }

    public void ClearMemoryTarget(Transform target)
    {
        if (memoryTarget != target)
        {
            return;
        }

        memoryTarget = null;
        ClearMemoryFlow();
    }

    public void SetEnemyDangerInfluence(float value)
    {
        EnemyDangerInfluence = Mathf.Clamp01(value);
    }

    public string GetDebugViewModeName()
    {
        return debugViewMode switch
        {
            DEBUG_VIEW_MODE.FINAL => "Final",
            DEBUG_VIEW_MODE.EDGE_ONLY => "Edge Only",
            DEBUG_VIEW_MODE.BASE_COLOR => "Base Color",
            DEBUG_VIEW_MODE.FLOW_UV => "Flow UV",
            _ => "Unknown"
        };
    }
}