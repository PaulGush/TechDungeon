public class AttackState : IState
{
    public AttackState(EnemyController enemyController)
    {
        _enemyController = enemyController;
    }
    
    private readonly EnemyController _enemyController;
    private EnemyMovement _movement;
    private EnemyShooting _shooting;
    
    public void Enter()
    {
        if (_movement == null)
        {
            _movement = _enemyController.GetService(typeof(EnemyMovement)) as EnemyMovement;
        }

        if (_shooting == null)
        {
            _shooting = _enemyController.GetService(typeof(EnemyShooting)) as EnemyShooting;
        }
    }

    public void Tick()
    {
        if (!_movement.IsTargetInRange())
        {
            _enemyController.StateMachine.ChangeState(_enemyController.StateMachine.SeekState);
        }
        else
        {
            _shooting.TryShoot();
        }
    }

    public void Exit()
    {
        
    }
}