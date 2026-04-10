using System;
using UnityEngine;

[Serializable]
public class DialogueLine
{
    public string SpeakerName;
    [TextArea(2, 5)]
    public string Text;
    public Sprite[] PortraitFrames;
    public float PortraitFrameRate = 6f;
    public DialogueSide Side;
}

public enum DialogueSide
{
    Left,
    Right
}
