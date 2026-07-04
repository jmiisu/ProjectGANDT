using UnityEngine;

namespace GANDT
{
    public class SearchState : EnemyStateBase
    {
        public override ENEMY_STATE StateType => ENEMY_STATE.SEARCH;

        private float searchTimer;

        public SearchState(EnemyAI enemy) : base(enemy)
        {
        }

        public override void Enter()
        {
            searchTimer = Enemy.SearchDuration;
            Enemy.SetDestination(Enemy.LastKnownPlayerPosition);
        }

        public override void Update()
        {
            if (Enemy.PlayerState != null && 
                !Enemy.PlayerState.IsEyesClosed 
                && Enemy.CanDetectPlayer())
            {
                Enemy.ChangeState(Enemy.Chase);
                return;
            }

            if (Enemy.HasReachedDestination(Enemy.SearchArrivalDistance))
            {
                Enemy.StopMovement();
            }

            searchTimer -= Time.deltaTime;

            if (searchTimer <= 0f)
            {
                Enemy.ChangeState(Enemy.Patrol);
            }
        }

        public override void Exit()
        {
            searchTimer = 0f;
        }
    }
}