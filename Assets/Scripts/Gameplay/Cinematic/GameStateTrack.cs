using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackBindingType(typeof(RoomManager))]
[TrackClipType(typeof(GameStateClip))]
public class GameStateTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<GameStateMixerBehaviour>.Create(graph, inputCount);
    }
}
