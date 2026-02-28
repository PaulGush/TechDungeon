using UnityEngine;

public class EnemyAnimationController : EntityAnimationController
{
    private static readonly int Heal = Animator.StringToHash("Heal");
    private static readonly int Hurt = Animator.StringToHash("Hurt");
    private static readonly int Dead = Animator.StringToHash("Dead");
    private static readonly int Attack = Animator.StringToHash("Attack");

    [SerializeField] private EnemyController m_enemyController;

    private EntityHealth m_health;

    private void Awake()
    {
        m_health = m_enemyController.Health;
    }

    private void OnEnable()
    {
        if (m_health != null)
        {
            m_health.OnHeal += OnHeal;
            m_health.OnTakeDamage += OnTakeDamage;
            m_health.OnDeath += OnDeath;
        }

        m_animator.SetBool(Dead, false);
    }

    private void OnDisable()
    {
        if (m_health != null)
        {
            m_health.OnHeal -= OnHeal;
            m_health.OnTakeDamage -= OnTakeDamage;
            m_health.OnDeath -= OnDeath;
        }
    }

    private void OnHeal()
    {
        m_animator.SetTrigger(Heal);
    }

    private void OnTakeDamage()
    {
        m_animator.SetTrigger(Hurt);
    }

    private void OnDeath()
    {
        m_animator.SetBool(Dead, true);
    }

    public void OnAttack()
    {
        if (m_animator.GetBool(Dead)) return;

        m_animator.SetTrigger(Attack);
    }

    public void OnDeathComplete()
    {
        m_enemyController.ReturnToPool();
    }
}
