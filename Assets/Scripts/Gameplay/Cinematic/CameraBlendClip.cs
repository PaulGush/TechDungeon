using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class CameraBlendClip : PlayableAsset, ITimelineClipAsset
{
    public CinemachineBlendDefinition.Styles BlendStyle = CinemachineBlendDefinition.Styles.EaseInOut;

    [Tooltip("Duration of the Cinemachine blend override while this clip is active.")]
    public float BlendDuration = 1.5f;

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<CameraBlendBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.BlendStyle = BlendStyle;
        behaviour.BlendDuration = BlendDuration;
        return playable;
    }
}
