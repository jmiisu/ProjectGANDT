using UnityEngine;

namespace GANDT
{
    public class PatrolState : EnemyStateBase
    {
        public override ENEMY_STATE StateType => ENEMY_STATE.PATROL;

        private float waitTimer;
        private bool isWaiting;

        public PatrolState(EnemyAI enemy) : base(enemy)
        {
        }

        public override void Enter()
        {
            waitTimer = 0f;
            isWaiting = false;

            MoveToCurrentPoint();
        }

        public override void Update()
        {
            if (Enemy.CanDetectPlayer())
            {
                Enemy.ChangeState(Enemy.Chase);
                return;
            }

            if (!Enemy.HasPatrolPoints())
            {
                Enemy.StopMovement();
                return;
            }

            if (isWaiting)
            {
                UpdateWait();
                return;
            }

            if (Enemy.HasReachedDestination(Enemy.PatrolArrivalDistance))
            {
                BeginWait();
            }
        }

        public override void Exit()
        {
            waitTimer = 0f;
            isWaiting = false;
        }

        private void MoveToCurrentPoint()
        {
            Transform patrolPoint = Enemy.GetCurrentPatrolPoint();

            if (patrolPoint == null)
            {
                Enemy.StopMovement();
                return;
            }

            Enemy.SetDestination(patrolPoint.position);
        }

        private void BeginWait()
        {
            isWaiting = true;
            waitTimer = Enemy.PatrolWaitDuration;

            Enemy.StopMovement();
        }

        private void UpdateWait()
        {
            waitTimer -= Time.deltaTime;

            if (waitTimer > 0f)
            {
                return;
            }
            
            isWaiting = false;
            Enemy.MoveToNextPatrolPoint();
            MoveToCurrentPoint();
        }
    }
}
