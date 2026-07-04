namespace GANDT
{
    public abstract class EnemyStateBase : IEnemyState
    {
        protected readonly EnemyAI Enemy;

        public abstract ENEMY_STATE StateType { get; }

        protected EnemyStateBase(EnemyAI enemy)
        {
            Enemy = enemy;
        }

        public virtual void Enter()
        {
            // Default implementation (can be overridden by derived classes)
        }

        public abstract void Update();

        public virtual void Exit()
        {

        }
    }
}