using System;

public class EnemyStateMachine
{
    public EnemyStateMachine(EnemyController enemyController)
    {
        _idleState = new IdleState(enemyController);
        SeekState = new SeekState(enemyController);
        AttackState = new AttackState(enemyController);
    }

    private IState _currentState;

    private IdleState _idleState;
    public SeekState SeekState;
    public AttackState AttackState;
    
    public Action<IState> OnStateChanged;

    public void ChangeState(IState newState)
    {
        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter();
        OnStateChanged?.Invoke(_currentState);
    }

    public void Initialize()
    {
        ChangeState(_idleState);
    }

    public void Tick()
    {
        _currentState?.Tick();
    }
}