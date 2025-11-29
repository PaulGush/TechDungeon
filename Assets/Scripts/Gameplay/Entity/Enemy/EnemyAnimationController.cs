using UnityEngine;

public class EnemyAnimationController : EntityAnimationController
{
    private static readonly int Heal = Animator.StringToHash("Heal");
    private static readonly int Hurt = Animator.StringToHash("Hurt");
    private static readonly int Dead = Animator.StringToHash("Dead");
    
    [SerializeField] private Enemy _enemy;

    private EntityHealth _health;
    private EnemyStateMachine _stateMachine;

    private void Awake()
    {
        _health = _enemy.GetService(typeof(EntityHealth)) as EntityHealth;
        _stateMachine = _enemy.GetService(typeof(EnemyStateMachine)) as EnemyStateMachine;
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
        _animator.SetBool(Dead, true);
    }
    
    private void OnStateChanged(IState newState)
    {
        //_animator.SetBool(newState.GetType().Name, true);
    }
}