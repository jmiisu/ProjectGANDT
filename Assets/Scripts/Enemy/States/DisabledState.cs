using UnityEngine;

namespace GANDT
{
    public class DisabledState : EnemyStateBase
    {
        public override ENEMY_STATE StateType => ENEMY_STATE.DISABLED;
        
        private float disabledTimer;

        public DisabledState(EnemyAI enemy) : base(enemy)
        {
        }

        public override void Enter()
        {
            disabledTimer = Enemy.DisabledDuration;
            Enemy.ResetMovementPath();
        }

        public override void Update()
        {
            disabledTimer -= Time.deltaTime;
            if (disabledTimer <= 0)
            {
                Enemy.ChangeState(Enemy.Patrol);
            }
        }

        public override void Exit()
        {
            disabledTimer = 0f;
        }
    }
}
