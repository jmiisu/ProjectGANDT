using UnityEngine;

namespace GANDT
{
    public class ChaseState : EnemyStateBase
    {
        public override ENEMY_STATE StateType => ENEMY_STATE.CHASE;

        public ChaseState(EnemyAI enemy) : base(enemy)
        {
        }

        public override void Enter()
        {
            Enemy.RememberPlayerPosition();

            if (Enemy.PlayerTransform != null)
            {
                Enemy.SetDestination(Enemy.PlayerTransform.position);
            }
        }

        public override void Update()
        {
            if (Enemy.PlayerState == null || Enemy.PlayerTransform == null)
            {
                Enemy.ChangeState(Enemy.Patrol);
                return;
            }

            if (Enemy.PlayerState.IsEyesClosed)
            {
                Enemy.ChangeState(Enemy.Search);
                return;
            }

            if (Enemy.GetDistanceToPlayer() > Enemy.LoseTargetRange)
            {
                Enemy.ChangeState(Enemy.Search);
                return;
            }

            if (!Enemy.HasLineOfSightToPlayer())
            {
                Enemy.ChangeState(Enemy.Search);
                return;
            }

            Enemy.RememberPlayerPosition();

            Enemy.SetDestination(Enemy.PlayerTransform.position);
        }

        public override void Exit()
        {
        }
    }
}
