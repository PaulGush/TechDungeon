using UnityEngine;

public class EnemyAnimationController : EntityAnimationController
{
    private static readonly int Heal = Animator.StringToHash("Heal");
    private static readonly int Hurt = Animator.StringToHash("Hurt");
    private static readonly int Dead = Animator.StringToHash("Dead");
    private static readonly int Attack = Animator.StringToHash("Attack");

    [SerializeField] private EnemyController _enemyController;

    private EntityHealth _health;

    private void Awake()
    {
        _health = _enemyController.GetService(typeof(EntityHealth)) as EntityHealth;
    }

    private void OnEnable()
    {
        if (_health != null)
        {
            _health.OnHeal += OnHeal;
            _health.OnTakeDamage += OnTakeDamage;
            _health.OnDeath += OnDeath;
        }
    }

    private void OnDisable()
    {
        if (_health != null)
        {
            _health.OnHeal -= OnHeal;
            _health.OnTakeDamage -= OnTakeDamage;
            _health.OnDeath -= OnDeath;
        }
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

    public void OnAttack()
    {
        if (_animator.GetBool(Dead)) return;
        
        _animator.SetTrigger(Attack);
    }

    public void OnDeathComplete()
    {
        _enemyController.gameObject.SetActive(false);
    }
}