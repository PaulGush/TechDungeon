using System;
using UnityEngine;

public class MechSuitBehavior : EnemyBehavior
{
    [SerializeField] private BossPhaseManager m_phaseManager;
    [SerializeField] private MechSuitShooting m_mechSuitShooting;
    [SerializeField] private BossSettings m_settings;

    public override IState CreateSeekState(EnemyController controller)
    {
        EnemyMovement movement = controller.Movement;

        return new EnemyState(
            enter: () => movement.CanMove = true,
            tick: () =>
            {
                if (movement.IsTargetInRange())
                    controller.StateMachine.ChangeState(controller.StateMachine.AttackState);
            },
            exit: () => movement.CanMove = false
        );
    }

    public override IState CreateAttackState(EnemyController controller)
    {
        EnemyMovement movement = controller.Movement;
        Action<int> phaseHandler = null;

        return new EnemyState(
            enter: () =>
            {
                movement.Strafe = true;
                ApplyPhaseMovement(movement);

                if (m_phaseManager != null)
                {
                    phaseHandler = _ => ApplyPhaseMovement(movement);
                    m_phaseManager.OnPhaseChanged += phaseHandler;
                }
            },
            tick: () =>
            {
                if (!movement.IsTargetInRange())
                {
                    controller.StateMachine.ChangeState(controller.StateMachine.SeekState);
                    return;
                }

                if (movement.HasLineOfSight())
                    m_mechSuitShooting.TryShoot();
            },
            exit: () =>
            {
                movement.Strafe = false;
                movement.CanMove = false;

                if (m_phaseManager != null && phaseHandler != null)
                {
                    m_phaseManager.OnPhaseChanged -= phaseHandler;
                    phaseHandler = null;
                }
            }
        );
    }

    private void ApplyPhaseMovement(EnemyMovement movement)
    {
        if (m_phaseManager == null) return;

        BossPhase phase = m_phaseManager.CurrentPhase;
        movement.CanMove = phase.AggressiveChase;
    }
}
