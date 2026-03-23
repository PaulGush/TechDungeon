using UnityEngine;

public class EnemyAnimationController : EntityAnimationController
{
    private static readonly int Heal = Animator.StringToHash("Heal");
    private static readonly int Hurt = Animator.StringToHash("Hurt");
    private static readonly int Dead = Animator.StringToHash("Dead");
    private static readonly int Attack = Animator.StringToHash("Attack");
    private static readonly int Rotation = Animator.StringToHash("Rotation");
    private static readonly int Active = Animator.StringToHash("Active");

    [SerializeField] private EnemyController m_enemyController;

    private EntityHealth m_health;
    private EnemyTargeting m_targeting;

    private bool m_hasHeal;
    private bool m_hasHurt;
    private bool m_hasRotation;
    private bool m_hasActive;

    private void Awake()
    {
        m_health = m_enemyController.Health;
        m_targeting = m_enemyController.Targeting;
    }

    private void OnEnable()
    {
        CacheParameters();

        if (m_health != null)
        {
            m_health.OnHeal += OnHeal;
            m_health.OnTakeDamage += OnTakeDamage;
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

        foreach (AnimatorControllerParameter param in m_animator.parameters)
        {
            int hash = param.nameHash;
            if (hash == Heal) m_hasHeal = true;
            else if (hash == Hurt) m_hasHurt = true;
            else if (hash == Rotation) m_hasRotation = true;
            else if (hash == Active) m_hasActive = true;
        }
    }

    private void OnDisable()
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

    private void Update()
    {
        Transform target = m_targeting.CurrentTarget;
        if (target == null) return;

        Vector3 diff = (target.position - m_enemyController.transform.position).normalized;
        float angle = Mathf.Repeat(-Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg, 360f);
        if (m_hasRotation) m_animator.SetFloat(Rotation, angle);
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
