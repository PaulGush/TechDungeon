public class SeekState : IState
{
    public SeekState(EnemyController enemyController)
    {
        _enemyController = enemyController;
    }
    
    private readonly EnemyController _enemyController;
    private EnemyMovement _movement;
    

    public void Enter()
    {
        if (_movement == null)
        {
            _movement = _enemyController.Movement;
        }
        
        _movement.CanMove = true;
    }

    public void Tick()
    {
        if (_movement.IsTargetInRange())
        {
            _enemyController.StateMachine.ChangeState(_enemyController.StateMachine.AttackState);
        }
    }

    public void Exit()
    {
        
    }
}