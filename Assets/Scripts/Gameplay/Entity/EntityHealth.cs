using System;
using UnityEngine;

public class EntityHealth : MonoBehaviour
{
    [SerializeField] protected int m_currentHealth;
    public int CurrentHealth => m_currentHealth;
    [SerializeField] protected int m_maxHealth;
    public int MaxHealth => m_maxHealth;
    public bool IsDead => m_currentHealth <= 0;
    
    public Action OnHeal;
    public Action OnTakeDamage;
    public Action OnDeath;
    public Action<int> OnHealthChanged;

    protected virtual void Start()
    {
        m_currentHealth = m_maxHealth;
        OnHealthChanged?.Invoke(m_currentHealth);
    }

    public virtual void TakeDamage(int damage)
    {
        m_currentHealth -= damage;
        OnTakeDamage?.Invoke();
        OnHealthChanged?.Invoke(m_currentHealth);
        
        if (m_currentHealth > 0)
            return;

        m_currentHealth = 0;
        OnHealthChanged?.Invoke(m_currentHealth);
        OnDeath?.Invoke();
    }

    public virtual bool Heal(int healAmount)
    {
        if (m_currentHealth + healAmount > m_maxHealth)
            return false;

        m_currentHealth += healAmount;
        OnHeal?.Invoke();
        OnHealthChanged?.Invoke(m_currentHealth);
        return true;
    }
}