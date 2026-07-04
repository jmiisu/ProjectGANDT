using System;

namespace GANDT
{
    public class EnemyFSM
    {
        public IEnemyState CurrentState { get; private set; }

        public event Action<ENEMY_STATE> OnStateChanged;

        public void ChangeState(IEnemyState nextState)
        {
            if (nextState == null || ReferenceEquals(CurrentState, nextState))
            {
                return;
            }

            CurrentState?.Exit();

            CurrentState = nextState;
            CurrentState.Enter();

            OnStateChanged?.Invoke(CurrentState.StateType);
        }

        public void Tick()
        {
            CurrentState?.Update();
        }

        public void Clear()
        {
            CurrentState?.Exit();
            CurrentState = null;
        }
    }
}
