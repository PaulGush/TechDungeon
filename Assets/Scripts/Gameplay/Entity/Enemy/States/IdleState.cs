public class IdleState : IState
{
    private EnemyController m_enemyController;

    public IdleState(EnemyController enemyController)
    {
        m_enemyController = enemyController;
    }

    public void Enter()
    {
        m_enemyController.Health.OnTakeDamage += () => m_enemyController.StateMachine.ChangeState(m_enemyController.StateMachine.SeekState);
    }

    public void Tick()
    {
        
    }

    public void Exit()
    {
        m_enemyController.Health.OnTakeDamage -= () => m_enemyController.StateMachine.ChangeState(m_enemyController.StateMachine.SeekState);
    }
}