public class AttackState : IState
{
    public AttackState(EnemyController enemyController)
    {
        m_enemyController = enemyController;
    }
    
    private readonly EnemyController m_enemyController;
    private EnemyMovement m_movement;
    private EnemyShooting m_shooting;
    
    public void Enter()
    {
        if (m_movement == null)
        {
            m_movement = m_enemyController.Movement;
        }

        if (m_shooting == null)
        {
            m_shooting = m_enemyController.Shooting;
        }
        
        m_movement.CanMove = false;
    }

    public void Tick()
    {
        if (!m_movement.IsTargetInRange())
        {
            m_enemyController.StateMachine.ChangeState(m_enemyController.StateMachine.SeekState);
        }
        else
        {
            m_shooting.TryShoot();
        }
    }

    public void Exit()
    {
        
    }
}