public class SeekState : IState
{
    private readonly EnemyController m_enemyController;
    private readonly EnemyMovement m_movement;

    public SeekState(EnemyController enemyController)
    {
        m_enemyController = enemyController;
        m_movement = enemyController.Movement;
    }

    public void Enter()
    {
        m_movement.CanMove = true;
    }

    public void Tick()
    {
        if (m_movement.IsTargetInRange())
        {
            m_enemyController.StateMachine.ChangeState(m_enemyController.StateMachine.AttackState);
        }
    }

    public void Exit()
    {

    }
}
