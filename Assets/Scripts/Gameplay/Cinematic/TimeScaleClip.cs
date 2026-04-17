using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class TimeScaleClip : PlayableAsset, ITimelineClipAsset
{
    [Tooltip("Time scale at the start of this clip.")]
    public float StartTimeScale = 1f;

    [Tooltip("Time scale at the end of this clip. The mixer lerps between start and end over the clip's duration.")]
    public float EndTimeScale = 1f;

    public ClipCaps clipCaps => ClipCaps.Blending;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<TimeScaleBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.StartTimeScale = StartTimeScale;
        behaviour.EndTimeScale = EndTimeScale;
        return playable;
    }
}
