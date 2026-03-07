public class AttackState : IState
{
    private readonly EnemyController m_enemyController;
    private readonly EnemyMovement m_movement;
    private readonly EnemyShooting m_shooting;

    public AttackState(EnemyController enemyController)
    {
        m_enemyController = enemyController;
        m_movement = enemyController.Movement;
        m_shooting = enemyController.Shooting;
    }

    public void Enter()
    {
        m_movement.CanMove = false;
        m_movement.Strafe = true;
    }

    public void Tick()
    {
        if (!m_movement.IsTargetInRange())
        {
            m_enemyController.StateMachine.ChangeState(m_enemyController.StateMachine.SeekState);
        }
        else if (m_movement.HasLineOfSight())
        {
            m_shooting.TryShoot();
        }
    }

    public void Exit()
    {
        m_movement.Strafe = false;
    }
}
