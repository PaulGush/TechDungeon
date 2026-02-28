using System;

public class IdleState : IState
{
    private readonly EnemyController m_enemyController;
    private Action m_onTakeDamageHandler;

    public IdleState(EnemyController enemyController)
    {
        m_enemyController = enemyController;
    }

    public void Enter()
    {
        m_onTakeDamageHandler = () => m_enemyController.StateMachine.ChangeState(m_enemyController.StateMachine.SeekState);
        m_enemyController.Health.OnTakeDamage += m_onTakeDamageHandler;
    }

    public void Tick()
    {

    }

    public void Exit()
    {
        m_enemyController.Health.OnTakeDamage -= m_onTakeDamageHandler;
    }
}
