using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackBindingType(typeof(CinemachineBrain))]
[TrackClipType(typeof(CameraBlendClip))]
public class CameraBlendTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<CameraBlendMixerBehaviour>.Create(graph, inputCount);
    }
}
