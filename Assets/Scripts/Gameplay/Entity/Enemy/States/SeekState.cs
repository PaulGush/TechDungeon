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
        _movement = _enemyController.GetService(typeof(EnemyMovement)) as EnemyMovement;
    }

    public void Tick()
    {
        if (_movement.IsTargetInRange())
        {
            _enemyController.StateMachine.ChangeState(_enemyController.StateMachine.AttackState);
        }

        _movement.MoveTowardTarget();
    }

    public void Exit()
    {
        
    }
}