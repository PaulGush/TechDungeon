using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class BossEventClip : PlayableAsset, ITimelineClipAsset
{
    public BossEventType EventType;
    [Tooltip("Only used when EventType is ScreenShake. Amplitude passed to the camera shake impulse.")]
    public float ShakeAmplitude = 1f;

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<BossEventBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.EventType = EventType;
        behaviour.ShakeAmplitude = ShakeAmplitude;
        return playable;
    }
}
