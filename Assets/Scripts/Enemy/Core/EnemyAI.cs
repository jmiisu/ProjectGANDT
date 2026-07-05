using System;
using UnityEngine;
using UnityEngine.AI;

namespace GANDT
{
    public class EnemyAI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private GANDT_PlayerState playerState;
        [SerializeField] private NavMeshAgent agent;

        [Header("Patrol")]
        [SerializeField] private Transform[] patrolPoints;

        [Min(0f)]
        [SerializeField] private float patrolWaitDuration = 1.5f;

        [Min(0.01f)]
        [SerializeField] private float patrolArrivalDistance = 0.3f;

        [Header("Detection")]
        [Min(0.01f)]
        [SerializeField] private float detectionRange = 8f;

        [Min(0.01f)]
        [SerializeField] private float loseTargetRange = 12f;

        [Min(0.01f)]
        [SerializeField] private float cryingDetectionRange = 15f;

        [Range(0f, 360f)]
        [SerializeField] private float fieldOfViewAngle = 120f;

        [SerializeField] private LayerMask obstacleMask;

        [Header("Search")]
        [Min(0f)]
        [SerializeField] private float searchDuration = 4f;

        [Min(0.01f)]
        [SerializeField] private float searchArrivalDistance = 0.5f;

        [Header("Disabled")]
        [Min(0f)]
        [SerializeField] private float disabledDuration = 5f;
        [SerializeField] private Renderer[] enemyRenderers;

        private EnemyFSM stateMachine;

        private int currentPatrolIndex;
        private Vector3 lastKnownPlayerPosition;

        public PatrolState Patrol { get; private set; }
        public ChaseState Chase { get; private set; }
        public SearchState Search { get; private set; }
        public DisabledState Disabled { get; private set; }

        public ENEMY_STATE CurrentStateType =>
            stateMachine?.CurrentState?.StateType
            ?? ENEMY_STATE.PATROL;

        public string CurrentStateName => CurrentStateType.ToString();

        public Transform PlayerTransform => playerTransform;
        public GANDT_PlayerState PlayerState => playerState;

        public float PatrolWaitDuration => patrolWaitDuration;
        public float PatrolArrivalDistance => patrolArrivalDistance;
        public float LoseTargetRange => loseTargetRange;
        public float SearchDuration => searchDuration;
        public float SearchArrivalDistance => searchArrivalDistance;
        public float DisabledDuration => disabledDuration;

        public Vector3 LastKnownPlayerPosition =>
            lastKnownPlayerPosition;

        public event Action<ENEMY_STATE> OnStateChanged
        {
            add
            {
                if (stateMachine != null)
                {
                    stateMachine.OnStateChanged += value;
                }
            }
            remove
            {
                if (stateMachine != null)
                {
                    stateMachine.OnStateChanged -= value;
                }
            }
        }

        private void Reset()
        {
            agent = GetComponent<NavMeshAgent>();
            FindPlayerReferences();
        }

        private void Awake()
        {
            FindReferences();
            CreateStates();

            if (enemyRenderers == null || enemyRenderers.Length == 0)
            {
                enemyRenderers = GetComponentsInChildren<Renderer>(true);
            }
        }

        private void OnEnable()
        {
            if (playerState != null)
            {
                playerState.OnCryingCompleted += HandleCryingCompleted;
            }
        }

        private void Start()
        {
            ChangeState(Patrol);
        }

        private void Update()
        {
            if (!CanRunAI())
            {
                StopMovement();
                return;
            }

            stateMachine?.Tick();
        }

        private void OnDisable()
        {
            if (playerState != null)
            {
                playerState.OnCryingCompleted -= HandleCryingCompleted;
            }

            stateMachine?.Clear();
        }

        private void OnValidate()
        {
            patrolWaitDuration = Mathf.Max(0f, patrolWaitDuration);

            patrolArrivalDistance = Mathf.Max(0.01f, patrolArrivalDistance);

            detectionRange = Mathf.Max(0.01f, detectionRange);

            loseTargetRange = Mathf.Max(detectionRange, loseTargetRange);

            cryingDetectionRange = Mathf.Max(0.01f, cryingDetectionRange);

            searchDuration = Mathf.Max(0f, searchDuration);
            searchArrivalDistance = Mathf.Max(0.01f, searchArrivalDistance);

            disabledDuration = Mathf.Max(0f, disabledDuration);
        }

        private void FindReferences()
        {
            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }

            if (playerTransform == null || playerState == null)
            {
                FindPlayerReferences();
            }
        }

        private void FindPlayerReferences()
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject == null)
            {
                return;
            }

            playerTransform = playerObject.transform;
            playerState = playerObject.GetComponent<GANDT_PlayerState>();
        }

        private void CreateStates()
        {
            stateMachine = new EnemyFSM();

            Patrol = new PatrolState(this);
            Chase = new ChaseState(this);
            Search = new SearchState(this);
            Disabled = new DisabledState(this);
        }

        public void ChangeState(IEnemyState nextState)
        {
            stateMachine?.ChangeState(nextState);
        }

        public bool CanRunAI()
        {
            return agent != null &&
                   agent.enabled &&
                   agent.isOnNavMesh &&
                   playerTransform != null &&
                   playerState != null;
        }

        public void SetVisualActive(bool isActive)
        {
            foreach (Renderer targetRenderer in enemyRenderers)
            {
                if (targetRenderer != null)
                {
                    targetRenderer.enabled = isActive;
                }
            }
        }
        // --------------------------------------------------
        // Patrol
        // --------------------------------------------------

        public bool HasPatrolPoints()
        {
            return patrolPoints != null && patrolPoints.Length > 0;
        }

        public Transform GetCurrentPatrolPoint()
        {
            if (!HasPatrolPoints())
            {
                return null;
            }

            currentPatrolIndex = Mathf.Clamp(currentPatrolIndex, 0, patrolPoints.Length - 1);

            return patrolPoints[currentPatrolIndex];
        }

        public void MoveToNextPatrolPoint()
        {
            if (!HasPatrolPoints())
            {
                return;
            }

            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }

        // --------------------------------------------------
        // Detection
        // --------------------------------------------------

        public bool CanDetectPlayer()
        {
            if (playerState == null || playerTransform == null)
            {
                return false;
            }

            if (playerState.IsEyesClosed)
            {
                return false;
            }

            float distance = GetDistanceToPlayer();

            if (playerState.IsCrying)
            {
                if (distance > cryingDetectionRange)
                {
                    return false;
                }

                RememberPlayerPosition();
                return true;
            }

            if (distance > detectionRange)
            {
                return false;
            }

            Vector3 directionToPlayer = playerTransform.position - transform.position;

            if (directionToPlayer.sqrMagnitude <= 0.0001f)
            {
                RememberPlayerPosition();
                return true;
            }

            float angle = Vector3.Angle(transform.forward, directionToPlayer.normalized);

            if (angle > fieldOfViewAngle * 0.5f)
            {
                return false;
            }

            if (!HasLineOfSightToPlayer())
            {
                return false;
            }

            RememberPlayerPosition();
            return true;
        }

        public bool HasLineOfSightToPlayer()
        {
            if (playerTransform == null)
            {
                return false;
            }

            Vector3 origin = transform.position + Vector3.up;

            Vector3 target = playerTransform.position + Vector3.up;

            Vector3 direction = target - origin;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return true;
            }

            return !Physics.Raycast(
                origin,
                direction.normalized,
                direction.magnitude,
                obstacleMask,
                QueryTriggerInteraction.Ignore
            );
        }

        public float GetDistanceToPlayer()
        {
            if (playerTransform == null)
            {
                return float.PositiveInfinity;
            }

            return Vector3.Distance(transform.position, playerTransform.position);
        }

        public void RememberPlayerPosition()
        {
            if (playerTransform != null)
            {
                lastKnownPlayerPosition = playerTransform.position;
            }
        }

        // --------------------------------------------------
        // Movement
        // --------------------------------------------------

        public bool SetDestination(Vector3 destination)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return false;
            }

            agent.isStopped = false;
            return agent.SetDestination(destination);
        }

        public void StopMovement()
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return;
            }

            agent.isStopped = true;
        }

        public void ResetMovementPath()
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return;
            }

            agent.isStopped = true;
            agent.ResetPath();
        }

        public bool HasReachedDestination(float arrivalDistance)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh || agent.pathPending)
            {
                return false;
            }

            if (agent.remainingDistance > arrivalDistance)
            {
                return false;
            }

            return !agent.hasPath || agent.velocity.sqrMagnitude <= 0.01f;
        }

        // --------------------------------------------------
        // Crying
        // --------------------------------------------------

        private void HandleCryingCompleted()
        {
            if (
                CurrentStateType == ENEMY_STATE.DISABLED ||
                playerTransform == null
            )
            {
                return;
            }

            if (GetDistanceToPlayer() <= cryingDetectionRange)
            {
                ChangeState(Disabled);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, loseTargetRange);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, cryingDetectionRange);

            if (!HasPatrolPoints())
            {
                return;
            }

            Gizmos.color = Color.green;

            for (int i = 0; i < patrolPoints.Length; i++)
            {
                Transform currentPoint = patrolPoints[i];

                if (currentPoint == null)
                {
                    continue;
                }

                Gizmos.DrawSphere(currentPoint.position, 0.15f);

                Transform nextPoint = patrolPoints[(i + 1) % patrolPoints.Length];

                if (nextPoint != null)
                {
                    Gizmos.DrawLine(currentPoint.position, nextPoint.position);
                }
            }
        }
    }
}