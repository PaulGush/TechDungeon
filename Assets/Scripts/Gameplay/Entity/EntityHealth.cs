using System;
using UnityEngine;

public class EntityHealth : MonoBehaviour
{
    [SerializeField] protected int _currentHealth;
    public int CurrentHealth => _currentHealth;
    [SerializeField] protected int _maxHealth;
    public int MaxHealth => _maxHealth;
    public bool IsDead => _currentHealth <= 0;
    
    public Action OnHeal;
    public Action OnTakeDamage;
    public Action OnDeath;
    public Action<int> OnHealthChanged;

    protected virtual void Start()
    {
        _currentHealth = _maxHealth;
        OnHealthChanged?.Invoke(_currentHealth);
    }

    public virtual void TakeDamage(int damage)
    {
        _currentHealth -= damage;
        OnTakeDamage?.Invoke();
        OnHealthChanged?.Invoke(_currentHealth);
        
        if (_currentHealth > 0)
            return;

        _currentHealth = 0;
        OnHealthChanged?.Invoke(_currentHealth);
        OnDeath?.Invoke();
    }

    public virtual bool Heal(int healAmount)
    {
        if (_currentHealth + healAmount > _maxHealth)
            return false;

        _currentHealth += healAmount;
        OnHeal?.Invoke();
        OnHealthChanged?.Invoke(_currentHealth);
        return true;
    }
}