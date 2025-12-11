public class IdleState : IState
{
    private EnemyController _enemyController;

    public IdleState(EnemyController enemyController)
    {
        _enemyController = enemyController;
    }

    public void Enter()
    {
        _enemyController.Health.OnTakeDamage += () => _enemyController.StateMachine.ChangeState(_enemyController.StateMachine.SeekState);
    }

    public void Tick()
    {
        
    }

    public void Exit()
    {
        _enemyController.Health.OnTakeDamage -= () => _enemyController.StateMachine.ChangeState(_enemyController.StateMachine.SeekState);
    }
}