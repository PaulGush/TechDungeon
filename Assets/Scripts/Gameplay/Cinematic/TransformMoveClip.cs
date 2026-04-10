using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class TransformMoveClip : PlayableAsset, ITimelineClipAsset
{
    public Vector3 StartPosition;
    public Vector3 EndPosition;
    [Tooltip("Use localPosition instead of world position.")]
    public bool UseLocalSpace;

    public ClipCaps clipCaps => ClipCaps.Blending;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<TransformMoveBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.StartPosition = StartPosition;
        behaviour.EndPosition = EndPosition;
        behaviour.UseLocalSpace = UseLocalSpace;
        return playable;
    }
}
