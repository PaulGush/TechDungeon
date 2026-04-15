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
        BossPhase phase = m_phaseManager != null ? m_phaseManager.CurrentPhase : null;
        BossAttackType attackType = phase != null ? phase.AttackType : BossAttackType.Projectile;

        // The missile barrage drives its own cadence from MissileBarrage's phase loop
        // (teleport → volley → teleport). TryShoot is a no-op for that phase so the
        // state machine's attack tick can't double-trigger it.
        if (attackType == BossAttackType.MissileBarrage)
            return;

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
                if (phase == null) break;
                if (m_flamethrower == null)
                {
                    Debug.LogWarning($"{nameof(MechSuitShooting)}: {nameof(m_flamethrower)} is not assigned for Flamethrower phase.", this);
                    break;
                }
                m_flamethrower.Fire(phase.FlameDamagePerTick, phase.FlameTickInterval, phase.FlameDuration);
                break;

            case BossAttackType.Burst:
                if (phase == null) break;
                if (m_burstAttack == null)
                {
                    Debug.LogWarning($"{nameof(MechSuitShooting)}: {nameof(m_burstAttack)} is not assigned for Burst phase.", this);
                    break;
                }
                m_burstAttack.Fire(phase.BurstProjectileCount);
                break;
        }

        m_lastTimeFired = Time.time;
    }
}
