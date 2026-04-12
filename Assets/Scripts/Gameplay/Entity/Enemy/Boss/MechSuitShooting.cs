using UnityEngine;

public class MechSuitShooting : EnemyShooting
{
    [Header("Boss")]
    [SerializeField] private BossPhaseManager m_phaseManager;

    [Header("Attack Components")]
    [SerializeField] private Flamethrower m_flamethrower;
    [SerializeField] private BurstAttack m_burstAttack;

    private BossSettings BossSettings => (BossSettings)m_settings;

    public override void TryShoot()
    {
        if (m_lastTimeFired + m_settings.FireRate > Time.time)
            return;

        m_animationController.OnAttack();
    }

    // Called from animation event
    public override void Shoot()
    {
        BossPhase phase = m_phaseManager != null ? m_phaseManager.CurrentPhase : null;
        BossAttackType attackType = phase != null ? phase.AttackType : BossAttackType.Projectile;

        switch (attackType)
        {
            case BossAttackType.Projectile:
                base.Shoot();
                return;

            case BossAttackType.Flamethrower:
                if (m_flamethrower != null && phase != null)
                    m_flamethrower.Fire(phase.FlameDamagePerTick, phase.FlameTickInterval, phase.FlameDuration);
                break;

            case BossAttackType.Burst:
                if (m_burstAttack != null && phase != null)
                    m_burstAttack.Fire(phase.BurstProjectileCount);
                break;
        }

        m_lastTimeFired = Time.time;
    }
}
