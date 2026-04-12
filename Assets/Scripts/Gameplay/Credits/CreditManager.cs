using System;
using UnityEngine;
using UnityServiceLocator;

public class CreditManager : MonoBehaviour
{
    [SerializeField] private int m_startingCredits;

    private int m_credits;
    public int Credits => m_credits;
    public Action<int> OnCreditsChanged;

    private void Awake()
    {
        ServiceLocator.Global.Register(this);
        m_credits = m_startingCredits;
    }

    public void AddCredits(int amount)
    {
        m_credits += amount;
        OnCreditsChanged?.Invoke(m_credits);
    }

    public bool TrySpend(int amount)
    {
        if (m_credits < amount)
            return false;

        m_credits -= amount;
        OnCreditsChanged?.Invoke(m_credits);
        return true;
    }

    public void Reset()
    {
        m_credits = m_startingCredits;
        OnCreditsChanged?.Invoke(m_credits);
    }
}
