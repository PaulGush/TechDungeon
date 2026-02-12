public class SeekState : IState
{
    public SeekState(EnemyController enemyController)
    {
        m_enemyController = enemyController;
    }
    
    private readonly EnemyController m_enemyController;
    private EnemyMovement m_movement;
    

    public void Enter()
    {
        if (m_movement == null)
        {
            m_movement = m_enemyController.Movement;
        }
        
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