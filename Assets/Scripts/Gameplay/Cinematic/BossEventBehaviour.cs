using UnityEngine.Playables;

public enum BossEventType
{
    PlayDeathAnimation,
    Kill,
    ReturnToPool,
    ScreenShake
}

public class BossEventBehaviour : PlayableBehaviour
{
    public BossEventType EventType;
    public float ShakeAmplitude;
}
