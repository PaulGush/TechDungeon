using UnityEngine;
using UnityEngine.Playables;

public class DialogueBehaviour : PlayableBehaviour
{
    public string SpeakerName;
    public string Text;
    public Sprite[] PortraitFrames;
    public float PortraitFrameRate;
    public DialogueSide Side;
    public bool WaitForInput;
}
