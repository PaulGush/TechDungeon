using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class GameStateClip : PlayableAsset, ITimelineClipAsset
{
    public bool PlayerInputActive;
    public bool PlayerGodMode;
    public bool BossVcamActive;
    public bool CameraConfinerActive;

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<GameStateBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.PlayerInputActive = PlayerInputActive;
        behaviour.PlayerGodMode = PlayerGodMode;
        behaviour.BossVcamActive = BossVcamActive;
        behaviour.CameraConfinerActive = CameraConfinerActive;
        return playable;
    }
}
