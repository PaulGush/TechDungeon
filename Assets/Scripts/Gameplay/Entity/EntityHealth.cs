using System;
using UnityEngine;

public class EntityHealth : MonoBehaviour
{
    [SerializeField] protected int m_currentHealth;
    public int CurrentHealth => m_currentHealth;
    [SerializeField] protected int m_maxHealth;
    public int MaxHealth => m_maxHealth;
    [SerializeField] protected int m_armor;
    public int Armor => m_armor;
    public bool IsDead => m_currentHealth <= 0;
    public bool IsGodMode { get; set; }

    public Action OnHeal;
    public Action OnTakeDamage;
    // Fires alongside OnTakeDamage with the post-armor damage amount. Subscribe here
    // when the amount matters (damage numbers, UI hit counters); use OnTakeDamage when
    // only the fact of a hit matters (hit flash, CA punch).
    public Action<int> OnTakeDamageAmount;
    public Action OnDeath;
    public Action<int> OnHealthChanged;

    /// <summary>
    /// Optional hook fired when an incoming hit would otherwise reduce health to zero.
    /// Return true to absorb the lethal damage and clamp the entity at 1 HP — death is
    /// suppressed and OnDeath does not fire. Used by BossDeathSequence to hold the boss
    /// alive while a death cutscene plays, then null itself out and call <see cref="Kill"/>
    /// to finish the kill cleanly afterwards.
    /// </summary>
    public Func<int, bool> DeathInterceptor;

    private int m_baseMaxHealth;
    private int m_baseArmor;

    protected virtual void Awake()
    {
        m_baseMaxHealth = m_maxHealth;
        m_baseArmor = m_armor;
    }

    protected virtual void Start()
    {
        m_currentHealth = m_maxHealth;
        OnHealthChanged?.Invoke(m_currentHealth);
    }

    public virtual void TakeDamage(int damage)
    {
        if (IsGodMode) return;

        int mitigated = Mathf.Max(damage - m_armor, 0);

        if (m_currentHealth > 0
            && m_currentHealth - mitigated <= 0
            && DeathInterceptor != null
            && DeathInterceptor(mitigated))
        {
            m_currentHealth = 1;
            OnTakeDamage?.Invoke();
            OnTakeDamageAmount?.Invoke(mitigated);
            OnHealthChanged?.Invoke(m_currentHealth);
            return;
        }

        m_currentHealth -= mitigated;
        OnTakeDamage?.Invoke();
        OnTakeDamageAmount?.Invoke(mitigated);
        OnHealthChanged?.Invoke(m_currentHealth);

        if (m_currentHealth > 0)
            return;

        m_currentHealth = 0;
        OnHealthChanged?.Invoke(m_currentHealth);
        OnDeath?.Invoke();
    }

    public void Kill()
    {
        if (m_currentHealth <= 0) return;
        m_currentHealth = 0;
        OnHealthChanged?.Invoke(m_currentHealth);
        OnDeath?.Invoke();
    }

    public void ResetHealth()
    {
        m_currentHealth = m_maxHealth;
        OnHealthChanged?.Invoke(m_currentHealth);
    }

    public void ResetToBase()
    {
        m_maxHealth = m_baseMaxHealth;
        m_armor = m_baseArmor;
        m_currentHealth = m_maxHealth;
        OnHealthChanged?.Invoke(m_currentHealth);
    }

    public virtual bool Heal(int healAmount)
    {
        if (m_currentHealth >= m_maxHealth)
            return false;

        m_currentHealth = Mathf.Min(m_currentHealth + healAmount, m_maxHealth);
        OnHeal?.Invoke();
        OnHealthChanged?.Invoke(m_currentHealth);
        return true;
    }

    public void IncreaseMaxHealth(int amount)
    {
        m_maxHealth += amount;
        m_currentHealth += amount;
        OnHealthChanged?.Invoke(m_currentHealth);
    }

    public void IncreaseArmor(int amount)
    {
        m_armor += amount;
    }
}
