using System;

public class EnemyState : IState
{
    private readonly Action m_enter;
    private readonly Action m_tick;
    private readonly Action m_exit;

    public EnemyState(Action enter = null, Action tick = null, Action exit = null)
    {
        m_enter = enter;
        m_tick = tick;
        m_exit = exit;
    }

    public void Enter() => m_enter?.Invoke();
    public void Tick() => m_tick?.Invoke();
    public void Exit() => m_exit?.Invoke();
}
