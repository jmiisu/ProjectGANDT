namespace GANDT
{
    public interface IEnemyState 
    {
        ENEMY_STATE StateType { get; }

        void Enter();
        void Update();
        void Exit();
    }
}