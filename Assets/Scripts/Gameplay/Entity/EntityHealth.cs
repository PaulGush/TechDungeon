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

    protected virtual void Start()
    {
        _currentHealth = _maxHealth;
    }

    public virtual void TakeDamage(int damage)
    {
        _currentHealth -= damage;
        OnTakeDamage?.Invoke();

        if (_currentHealth > 0)
            return;

        _currentHealth = 0;
        OnDeath?.Invoke();
    }

    public virtual void Heal(int healAmount)
    {
        Mathf.Clamp(_currentHealth += healAmount, 0, _maxHealth);
        OnHeal?.Invoke();
    }
}