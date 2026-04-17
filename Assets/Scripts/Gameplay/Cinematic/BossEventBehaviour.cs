using UnityEngine.Playables;

public enum BossEventType
{
    PlayDeathAnimation,
    Kill,
    ReturnToPool
}

public class BossEventBehaviour : PlayableBehaviour
{
    public BossEventType EventType;
}
