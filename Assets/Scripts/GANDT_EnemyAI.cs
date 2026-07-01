using UnityEngine;
using UnityEngine.AI;

public class GANDT_EnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private GANDT_PlayerState playerState;
    [SerializeField] private NavMeshAgent agent;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float loseTargetRange = 12f;
    [SerializeField] private float cryingDetectionRange = 15f;
    [SerializeField] private float fieldOfViewAngle = 120f;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Search")]
    [SerializeField] private float searchDuration = 4f;
    [SerializeField] private float searchStoppingDistance = 0.5f;

    [Header("Disable")]
    [SerializeField] private float disabledDuration = 5f;

    public ENEMY_STATE CurrentState { get; private set; }

    private Vector3 lastKnownPlayerPosition;
    private float stateTimer;

    private void Reset()
    {
        agent = GetComponent<NavMeshAgent>();

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            playerState = playerObject.GetComponent<GANDT_PlayerState>();
        }
    }

    void Awake()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }
        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
        }

        if (playerState == null && playerTransform != null)
        {
            playerState = playerTransform.GetComponent<GANDT_PlayerState>();
        }
    }

    private void OnEnable()
    {
        if (playerState != null)
        {
            playerState.OnCryingCompleted += HandleCryingCompleted;
        }
    }

    private void OnDisable()
    {
        if (playerState != null)
        {
            playerState.OnCryingCompleted -= HandleCryingCompleted;
        }
    }

    private void Start()
    {
        CurrentState = ENEMY_STATE.PATROL;
        agent.isStopped = true; // 초기에는 정지 상태로 시작
    }


    private void Update()
    {
        if (playerState == null || playerTransform == null)
        {
            return;
        }

        switch (CurrentState)
        {
            case ENEMY_STATE.PATROL:
                UpdatePatrol();
                break;
            case ENEMY_STATE.CHASE:
                UpdateChase();
                break;
            case ENEMY_STATE.SEARCH:
                UpdateSearch();
                break;
            case ENEMY_STATE.DISABLED:
                UpdateDisabled();
                break;
        }
    }

    private void UpdatePatrol()
    {
        agent.isStopped = true; // 임시로 정지 상태

        if (CanDetectPlayer())
        {
            ChangeState(ENEMY_STATE.CHASE);
        }
    }

    private void UpdateChase()
    {
        if (playerState.IsEyesClosed)
        {
            ChangeState(ENEMY_STATE.SEARCH);
            return;
        }

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance > loseTargetRange || !HasLineOfSightToPlayer())
        {
            ChangeState(ENEMY_STATE.SEARCH);
            return;
        }

        lastKnownPlayerPosition = playerTransform.position;

        agent.isStopped = false;
        agent.SetDestination(playerTransform.position);
    }

    private void UpdateSearch()
    {
        if (agent.isOnNavMesh)
        {
            agent.SetDestination(lastKnownPlayerPosition);
        }

        stateTimer -= Time.deltaTime;

        // 눈을 다시 떴고 적의 시야에 들어오면 재추적
        if (!playerState.IsEyesClosed && CanDetectPlayer())
        {
            ChangeState(ENEMY_STATE.CHASE);
            return;
        }

        if (stateTimer <= 0f)
        {
            ChangeState(ENEMY_STATE.PATROL);
        }
    }

    private void UpdateDisabled()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            ChangeState(ENEMY_STATE.PATROL);
        }
    }

    private bool CanDetectPlayer()
    {
        if (playerState.IsEyesClosed)
        {
            return false;
        }

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        float currentDetectionRange =
            playerState.IsCrying ? cryingDetectionRange : detectionRange;

        if (distance > currentDetectionRange)
        {
            return false;
        }

        // 울고 있을 때는 시야각 밖이라도 소리를 통해 감지한다.
        if (playerState.IsCrying)
        {
            lastKnownPlayerPosition = playerTransform.position;
            return true;
        }

        Vector3 directionToPlayer =
            (playerTransform.position - transform.position).normalized;

        float angle = Vector3.Angle(transform.forward, directionToPlayer);

        if (angle > fieldOfViewAngle * 0.5f)
        {
            return false;
        }

        if (!HasLineOfSightToPlayer())
        {
            return false;
        }

        lastKnownPlayerPosition = playerTransform.position;
        return true;
    }

    private bool HasLineOfSightToPlayer()
    {
        Vector3 origin = transform.position + Vector3.up;
        Vector3 target = playerTransform.position + Vector3.up;
        Vector3 direction = target - origin;

        return !Physics.Raycast(
            origin,
            direction.normalized,
            direction.magnitude,
            obstacleMask
        );
    }

    private void ChangeState(ENEMY_STATE nextState)
    {
        if (CurrentState == nextState)
        {
            return;
        }

        CurrentState = nextState;

        switch (nextState)
        {
            case ENEMY_STATE.PATROL:
                agent.isStopped = true;
                break;

            case ENEMY_STATE.CHASE:
                agent.isStopped = false;
                break;

            case ENEMY_STATE.SEARCH:
                stateTimer = searchDuration;
                agent.isStopped = false;
                agent.SetDestination(lastKnownPlayerPosition); // 플레이어를 실제로 보고 있을 때만 갱신
                break;

            case ENEMY_STATE.DISABLED:
                stateTimer = disabledDuration;
                agent.isStopped = true;
                agent.ResetPath();
                break;
        }
    }

    private void HandleCryingCompleted()
    {
        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance <= cryingDetectionRange)
        {
            ChangeState(ENEMY_STATE.DISABLED);
        }
    }
}
