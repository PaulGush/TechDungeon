using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackBindingType(typeof(DialogueBoxUI))]
[TrackClipType(typeof(DialogueClip))]
public class DialogueTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        // Push clip display names from the dialogue text for readability in the Timeline editor
        foreach (var clip in GetClips())
        {
            var dialogueClip = clip.asset as DialogueClip;
            if (dialogueClip != null && !string.IsNullOrEmpty(dialogueClip.SpeakerName))
            {
                clip.displayName = dialogueClip.SpeakerName;
            }
        }

        return ScriptPlayable<DialogueMixerBehaviour>.Create(graph, inputCount);
    }
}
