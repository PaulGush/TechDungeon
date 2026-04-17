using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackBindingType(typeof(BossDeathTrigger))]
[TrackClipType(typeof(BossEventClip))]
public class BossEventTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<BossEventMixerBehaviour>.Create(graph, inputCount);
    }
}
