using System;
using UnityEngine;

public abstract class EnemyBehavior : MonoBehaviour
{
    public virtual IState CreateIdleState(EnemyController controller)
    {
        Action onTakeDamageHandler = null;

        return new EnemyState(
            enter: () =>
            {
                onTakeDamageHandler = () =>
                    controller.StateMachine.ChangeState(controller.StateMachine.SeekState);
                controller.Health.OnTakeDamage += onTakeDamageHandler;
            },
            exit: () =>
            {
                controller.Health.OnTakeDamage -= onTakeDamageHandler;
            }
        );
    }

    public abstract IState CreateSeekState(EnemyController controller);
    public abstract IState CreateAttackState(EnemyController controller);
}
