using UnityEngine;

public class GANDT_EnemyComponent : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GANDT_PlayerStatus playerStatus;
    [SerializeField] private GANDT_PostProcessController postProcessController;
    [SerializeField] private GANDT_EnemyAI enemyAI;

    [Header("Danger Zone")]
    [Min(0.01f)]
    [SerializeField] private float dangerRadius = 5f;

    [Min(0f)]
    [SerializeField] private float sanityDrainPerSecond = 0.12f;

    [Header("Gizmo")]
    [SerializeField] private bool drawSolidGizmo = true;

    public bool IsPlayerInside { get; private set; }
    public float DangerInfluence { get; private set; }

    private void Reset()
    {
        enemyAI = GetComponent<GANDT_EnemyAI>();
        postProcessController =
            FindFirstObjectByType<GANDT_PostProcessController>();

        FindPlayer();
    }

    private void Awake()
    {
        FindMissingReferences();
    }

    private void OnValidate()
    {
        dangerRadius = Mathf.Max(0.01f, dangerRadius);
        sanityDrainPerSecond = Mathf.Max(0f, sanityDrainPerSecond);
    }

    private void Update()
    {
        if (player == null || playerStatus == null || postProcessController == null)
        {
            ClearDangerState();
            return;
        }

        UpdateDangerState();
        ApplyDangerEffect();
    }

    private void OnDisable()
    {
        ClearDangerState();
    }

    private void FindMissingReferences()
    {
        if (enemyAI == null)
        {
            enemyAI = GetComponent<GANDT_EnemyAI>();
        }

        if (postProcessController == null)
        {
            postProcessController = FindFirstObjectByType<GANDT_PostProcessController>();
        }

        if (player == null || playerStatus == null)
        {
            FindPlayer();
        }
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            return;
        }

        player = playerObject.transform;
        playerStatus = playerObject.GetComponent<GANDT_PlayerStatus>();
    }

    private void UpdateDangerState()
    {
        if (enemyAI != null && enemyAI.CurrentState == ENEMY_STATE.DISABLED)
        {
            IsPlayerInside = false;
            DangerInfluence = 0f;
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        IsPlayerInside = distance <= dangerRadius;

        if (!IsPlayerInside)
        {
            DangerInfluence = 0f;
            return;
        }

        float normalizedDistance = Mathf.Clamp01(distance / dangerRadius);

        DangerInfluence = 1f - normalizedDistance;
    }

    private void ApplyDangerEffect()
    {
        if (IsPlayerInside)
        {
            float drainAmount = sanityDrainPerSecond * DangerInfluence * Time.deltaTime;

            playerStatus.AddSanity(-drainAmount);
        }

        postProcessController.SetEnemyDangerInfluence(DangerInfluence);
    }

    private void ClearDangerState()
    {
        IsPlayerInside = false;
        DangerInfluence = 0f;

        if (postProcessController != null)
        {
            postProcessController.SetEnemyDangerInfluence(0f);
        }
    }

    private void OnDrawGizmos()
    {
        Color wireColor = IsPlayerInside ? Color.red : Color.yellow;

        Gizmos.color = wireColor;
        Gizmos.DrawWireSphere(transform.position, dangerRadius);

        if (!drawSolidGizmo)
        {
            return;
        }

        Gizmos.color = IsPlayerInside
            ? new Color(1f, 0f, 0f, 0.2f)
            : new Color(1f, 1f, 0f, 0.12f);

        Gizmos.DrawSphere(transform.position, dangerRadius);
    }
}