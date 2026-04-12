using PlayerObject;
using UnityEngine;
using UnityServiceLocator;

public class TurretBehavior : EnemyBehavior
{
    public override IState CreateSeekState(EnemyController controller)
    {
        EnemyMovement movement = controller.Movement;
        float alertDuration = movement.Settings.AlertDuration;
        float alertTimer = 0f;

        return new EnemyState(
            enter: () =>
            {
                alertTimer = 0f;

                // Acquire player if no target (e.g. activated by taking damage from range)
                if (controller.Targeting.CurrentTarget == null)
                {
                    if (ServiceLocator.Global.TryGet(out PlayerMovementController player))
                        controller.Targeting.SetTarget(player.transform);
                }
            },
            tick: () =>
            {
                if (movement.IsTargetInRange())
                {
                    controller.StateMachine.ChangeState(controller.StateMachine.AttackState);
                    return;
                }

                if (alertDuration > 0f)
                {
                    alertTimer += Time.deltaTime;
                    if (alertTimer >= alertDuration)
                    {
                        controller.Targeting.SetTarget(null);
                        controller.StateMachine.Reset();
                    }
                }
            }
        );
    }

    public override IState CreateAttackState(EnemyController controller)
    {
        EnemyMovement movement = controller.Movement;
        EnemyShooting shooting = controller.Shooting;

        return new EnemyState(
            tick: () =>
            {
                if (!movement.IsTargetInRange())
                    controller.StateMachine.ChangeState(controller.StateMachine.SeekState);
                else if (movement.HasLineOfSight())
                    shooting.TryShoot();
            }
        );
    }
}
