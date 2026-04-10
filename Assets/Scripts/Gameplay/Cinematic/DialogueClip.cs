using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class DialogueClip : PlayableAsset, ITimelineClipAsset
{
    [Header("Speaker")]
    public string SpeakerName;
    public DialogueSide Side;

    [Header("Portrait")]
    [Tooltip("Sprite frames for the portrait. A single frame works as a static image.")]
    public Sprite[] PortraitFrames;
    [Tooltip("Frames per second for animated portraits.")]
    public float PortraitFrameRate = 6f;

    [Header("Dialogue")]
    [TextArea(2, 5)]
    public string Text;

    [Header("Behaviour")]
    [Tooltip("Pause the timeline after typing finishes until the player presses a button.")]
    public bool WaitForInput = true;

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<DialogueBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.SpeakerName = SpeakerName;
        behaviour.Text = Text;
        behaviour.PortraitFrames = PortraitFrames;
        behaviour.PortraitFrameRate = PortraitFrameRate;
        behaviour.Side = Side;
        behaviour.WaitForInput = WaitForInput;
        return playable;
    }
}
