using UnityEngine;
using UnityEngine.Playables;

public class DialogueMixerBehaviour : PlayableBehaviour
{
    private const float TypingPortion = 0.85f;

    private DialogueBoxUI m_dialogueBox;
    private bool m_hasActiveClip;
    private int m_lastClipHash = -1;
    private bool m_pausedForInput;
    private bool m_lineShown;
    private bool m_waitForInputPending;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        m_dialogueBox = playerData as DialogueBoxUI;
        if (m_dialogueBox == null) return;

        int inputCount = playable.GetInputCount();
        DialogueBehaviour activeBehaviour = null;
        ScriptPlayable<DialogueBehaviour> activePlayable = default;
        int activeHash = -1;
        float activeWeight = 0f;

        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            if (weight > 0f)
            {
                activePlayable = (ScriptPlayable<DialogueBehaviour>)playable.GetInput(i);
                activeBehaviour = activePlayable.GetBehaviour();
                activeHash = i;
                activeWeight = weight;
                break;
            }
        }

        // Typing finished within the current clip — pause here while it's still active
        if (m_waitForInputPending && !m_pausedForInput && !m_dialogueBox.IsTyping)
        {
            m_dialogueBox.CompleteLine();
            var rootPlayable = playable.GetGraph().GetRootPlayable(0);
            rootPlayable.SetSpeed(0);
            m_pausedForInput = true;
            m_waitForInputPending = false;
            return;
        }

        if (activeBehaviour == null)
        {
            if (m_hasActiveClip)
            {
                m_dialogueBox.SetAlpha(0f);
                m_hasActiveClip = false;
                m_lastClipHash = -1;
                m_lineShown = false;
            }
            return;
        }

        // Apply clip weight as panel opacity
        m_dialogueBox.SetAlpha(activeWeight);

        // New clip became active
        if (activeHash != m_lastClipHash)
        {
            m_lastClipHash = activeHash;
            m_hasActiveClip = true;
            m_pausedForInput = false;
            m_lineShown = false;

            float clipDuration = (float)activePlayable.GetDuration();
            string text = activeBehaviour.Text;
            // Typing fills 85% of the clip, leaving the rest as buffer for the pause to trigger
            float typingDuration = Mathf.Max(clipDuration * TypingPortion, 0.01f);
            float typingSpeed = text.Length / typingDuration;

            var line = new DialogueLine
            {
                SpeakerName = activeBehaviour.SpeakerName,
                Text = text,
                PortraitFrames = activeBehaviour.PortraitFrames,
                PortraitFrameRate = activeBehaviour.PortraitFrameRate,
                Side = activeBehaviour.Side
            };
            m_dialogueBox.ShowLine(line, typingSpeed);
            m_lineShown = true;
            m_waitForInputPending = activeBehaviour.WaitForInput;
        }
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        if (m_dialogueBox != null)
        {
            m_dialogueBox.Hide();
            m_hasActiveClip = false;
        }
    }
}
