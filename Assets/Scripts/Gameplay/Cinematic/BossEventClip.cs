using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class BossEventClip : PlayableAsset, ITimelineClipAsset
{
    public BossEventType EventType;

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<BossEventBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.EventType = EventType;
        return playable;
    }
}
