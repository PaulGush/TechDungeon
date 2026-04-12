using System;

public class EnemyStateMachine
{
    private IState m_currentState;
    private readonly IState m_idleState;

    public IState SeekState;
    public IState AttackState;

    public Action<IState> OnStateChanged;

    public EnemyStateMachine(IState idle, IState seek, IState attack)
    {
        m_idleState = idle;
        SeekState = seek;
        AttackState = attack;
    }

    public void ChangeState(IState newState)
    {
        m_currentState?.Exit();
        m_currentState = newState;
        m_currentState.Enter();
        OnStateChanged?.Invoke(m_currentState);
    }

    public void Initialize()
    {
        ChangeState(m_idleState);
    }

    public void Reset()
    {
        m_currentState?.Exit();
        m_currentState = null;
        Initialize();
    }

    public void Tick()
    {
        m_currentState?.Tick();
    }
}
