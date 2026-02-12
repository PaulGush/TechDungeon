using System;

public class EnemyStateMachine
{
    public EnemyStateMachine(EnemyController enemyController)
    {
        m_idleState = new IdleState(enemyController);
        SeekState = new SeekState(enemyController);
        AttackState = new AttackState(enemyController);
    }

    private IState m_currentState;

    private IdleState m_idleState;
    public SeekState SeekState;
    public AttackState AttackState;
    
    public Action<IState> OnStateChanged;

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

    public void Tick()
    {
        m_currentState?.Tick();
    }
}