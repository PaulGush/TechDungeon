using UnityEngine;

public class EnemyAnimationController : EntityAnimationController
{
    private static readonly int Heal = Animator.StringToHash("Heal");
    private static readonly int Hurt = Animator.StringToHash("Hurt");
    private static readonly int Dead = Animator.StringToHash("Dead");
    private static readonly int Attack = Animator.StringToHash("Attack");
    private static readonly int AttackIndex = Animator.StringToHash("AttackIndex");
    private static readonly int Rotation = Animator.StringToHash("Rotation");
    private static readonly int Active = Animator.StringToHash("Active");

    [SerializeField] protected EnemyController m_enemyController;

    [Tooltip("If true, the default Dead animator state and OnDeathComplete pool return are skipped. The owning entity is responsible for its own death visualization and pool return (e.g., via BossDeathSequence).")]
    [SerializeField] private bool m_suppressDefaultDeathAnimation;

    protected EntityHealth m_health;
    protected EnemyTargeting m_targeting;

    private bool m_hasHeal;
    private bool m_hasHurt;
    private bool m_hasRotation;
    private bool m_hasActive;
    private bool m_hasAttackIndex;

    public int CurrentAttackIndex { get; set; }

    private void Awake()
    {
        m_health = m_enemyController.Health;
        m_targeting = m_enemyController.Targeting;
    }

    protected virtual void OnEnable()
    {
        CacheParameters();

        if (m_health != null)
        {
            m_health.OnHeal += OnHeal;
            m_health.OnTakeDamage += OnTakeDamage;
            if (!m_suppressDefaultDeathAnimation)
                m_health.OnDeath += OnDeath;
        }

        if (m_targeting != null)
        {
            m_targeting.OnTargetChanged += OnTargetChanged;
        }

        m_animator.SetBool(Dead, false);
        if (m_hasActive) m_animator.SetBool(Active, false);
        if (m_hasRotation) m_animator.SetFloat(Rotation, 0f);
    }

    private void CacheParameters()
    {
        m_hasHeal = false;
        m_hasHurt = false;
        m_hasRotation = false;
        m_hasActive = false;
        m_hasAttackIndex = false;

        foreach (AnimatorControllerParameter param in m_animator.parameters)
        {
            int hash = param.nameHash;
            if (hash == Heal) m_hasHeal = true;
            else if (hash == Hurt) m_hasHurt = true;
            else if (hash == Rotation) m_hasRotation = true;
            else if (hash == Active) m_hasActive = true;
            else if (hash == AttackIndex) m_hasAttackIndex = true;
        }
    }

    protected virtual void OnDisable()
    {
        if (m_health != null)
        {
            m_health.OnHeal -= OnHeal;
            m_health.OnTakeDamage -= OnTakeDamage;
            m_health.OnDeath -= OnDeath;
        }

        if (m_targeting != null)
        {
            m_targeting.OnTargetChanged -= OnTargetChanged;
        }
    }

    protected virtual void Update()
    {
        if (!m_hasRotation) return;

        Transform target = m_targeting.CurrentTarget;
        if (target == null) return;

        Vector2 diff = ((Vector2)(target.position - m_enemyController.transform.position)).normalized;
        m_animator.SetFloat(Rotation, MathUtilities.CalculateSpriteFacingAngleDegrees(diff));
    }

    private void OnTargetChanged(Transform target)
    {
        if (m_hasActive) m_animator.SetBool(Active, target != null);
    }

    private void OnHeal()
    {
        if (m_hasHeal) m_animator.SetTrigger(Heal);
    }

    private void OnTakeDamage()
    {
        if (m_hasHurt) m_animator.SetTrigger(Hurt);
    }

    protected virtual void OnDeath()
    {
        m_animator.SetBool(Dead, true);
    }

    /// <summary>
    /// Fires the death animation manually. Used by entities that suppress the default
    /// OnDeath subscription (e.g., bosses with a death cutscene) and need to trigger
    /// the death animator state at a controlled point in their own sequence.
    /// </summary>
    public void PlayDeathAnimation()
    {
        OnDeath();
    }

    public void OnAttack()
    {
        if (m_animator.GetBool(Dead)) return;
        if (m_hasAttackIndex) m_animator.SetFloat(AttackIndex, CurrentAttackIndex);
        m_animator.SetTrigger(Attack);
    }

    public void OnDeathComplete()
    {
        // When the default death flow is suppressed the owning entity (e.g., BossDeathSequence)
        // is in charge of pool return, so this animation-event callback must no-op or it will
        // race against the owner's cleanup and double-return the object to the pool.
        if (m_suppressDefaultDeathAnimation) return;
        m_enemyController.ReturnToPool();
    }
}
