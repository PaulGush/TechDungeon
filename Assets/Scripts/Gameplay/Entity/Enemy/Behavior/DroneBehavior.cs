public class DroneBehavior : EnemyBehavior
{
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
        EnemyShooting shooting = controller.Shooting;

        return new EnemyState(
            enter: () =>
            {
                movement.CanMove = false;
                movement.Strafe = true;
            },
            tick: () =>
            {
                if (!movement.IsTargetInRange())
                    controller.StateMachine.ChangeState(controller.StateMachine.SeekState);
                else if (movement.HasLineOfSight())
                    shooting.TryShoot();
            },
            exit: () => movement.Strafe = false
        );
    }
}
