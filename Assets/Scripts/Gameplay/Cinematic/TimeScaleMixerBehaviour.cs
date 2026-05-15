using UnityEngine;
using UnityEngine.Playables;

public class TimeScaleMixerBehaviour : PlayableBehaviour
{
    private float m_originalTimeScale;
    private float m_originalFixedDeltaTime;
    private bool m_originalCaptured;
    private float m_lastWrittenScale;
    private bool m_hasWritten;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (!m_originalCaptured)
        {
            m_originalTimeScale = Time.timeScale;
            m_originalFixedDeltaTime = Time.fixedDeltaTime;
            m_originalCaptured = true;
        }

        int inputCount = playable.GetInputCount();
        float blendedScale = 0f;
        float totalWeight = 0f;

        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            if (weight <= 0f) continue;

            var input = (ScriptPlayable<TimeScaleBehaviour>)playable.GetInput(i);
            var behaviour = input.GetBehaviour();
            float t = (float)(input.GetTime() / input.GetDuration());

            float scale = Mathf.Lerp(behaviour.StartTimeScale, behaviour.EndTimeScale, t);
            blendedScale += scale * weight;
            totalWeight += weight;
        }

        if (totalWeight > 0f)
        {
            m_lastWrittenScale = blendedScale;
            m_hasWritten = true;
        }

        // Hold the last-written value during gaps so timeScale doesn't snap back.
        if (m_hasWritten)
        {
            Time.timeScale = m_lastWrittenScale;
            Time.fixedDeltaTime = m_originalFixedDeltaTime * m_lastWrittenScale;
        }
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        if (m_originalCaptured)
        {
            Time.timeScale = m_originalTimeScale;
            Time.fixedDeltaTime = m_originalFixedDeltaTime;
        }
    }
}
