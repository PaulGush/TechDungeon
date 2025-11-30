using UnityEngine;

public class EnemyAnimationController : EntityAnimationController
{
    private static readonly int Heal = Animator.StringToHash("Heal");
    private static readonly int Hurt = Animator.StringToHash("Hurt");
    private static readonly int Dead = Animator.StringToHash("Dead");
    private static readonly int Attack = Animator.StringToHash("Attack");

    [SerializeField] private EnemyController _enemyController;

    private EntityHealth _health;
    private EnemyStateMachine _stateMachine;

    private void Awake()
    {
        _health = _enemyController.GetService(typeof(EntityHealth)) as EntityHealth;
        _stateMachine = _enemyController.StateMachine;
    }

    private void OnEnable()
    {
        if (_health != null)
        {
            _health.OnHeal += OnHeal;
            _health.OnTakeDamage += OnTakeDamage;
            _health.OnDeath += OnDeath;
        }

        if (_stateMachine != null)
        {
            _stateMachine.OnStateChanged += OnStateChanged;
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

        if (_stateMachine != null)
        {
            _stateMachine.OnStateChanged -= OnStateChanged;
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
        _animator.SetBool(Attack, false);
        _animator.SetBool(Dead, true);
    }
    
    private void OnStateChanged(IState newState)
    {
        if (newState.GetType() == typeof(AttackState))
        {
            _animator.SetBool(Attack, true);
            return;
        }
        _animator.SetBool(Attack, false);
    }
}