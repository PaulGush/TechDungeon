using UnityEngine;

public class EnemyAnimationController : EntityAnimationController
{
    private static readonly int Heal = Animator.StringToHash("Heal");
    private static readonly int Hurt = Animator.StringToHash("Hurt");
    private static readonly int Dead = Animator.StringToHash("Dead");
    
    [SerializeField] private EntityHealth _health;

    private void OnEnable()
    {
        _health.OnHeal += OnHeal;
        _health.OnTakeDamage += OnTakeDamage;
        _health.OnDeath += OnDeath;
    }

    private void OnDisable()
    {
        _health.OnHeal -= OnHeal;
        _health.OnTakeDamage -= OnTakeDamage;
        _health.OnDeath -= OnDeath;
    }

    private void OnHeal()
    {
        _animator.SetTrigger(Heal);
    }

    private void OnTakeDamage()
    {
        _animator.SetTrigger(Hurt);
    }

    private void OnDeath()
    {
        _animator.SetBool(Dead, true);
    }
}