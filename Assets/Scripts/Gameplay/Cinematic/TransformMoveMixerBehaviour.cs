using UnityEngine;
using UnityEngine.Playables;

public class TransformMoveMixerBehaviour : PlayableBehaviour
{
    private Vector3 m_originalPosition;
    private bool m_originalCaptured;
    private Transform m_target;
    private bool m_useLocalSpace;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        m_target = playerData as Transform;
        if (m_target == null) return;

        if (!m_originalCaptured)
        {
            m_originalPosition = m_target.localPosition;
            m_originalCaptured = true;
        }

        int inputCount = playable.GetInputCount();
        Vector3 blendedPosition = Vector3.zero;
        float totalWeight = 0f;

        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            if (weight <= 0f) continue;

            var input = (ScriptPlayable<TransformMoveBehaviour>)playable.GetInput(i);
            var behaviour = input.GetBehaviour();
            float t = (float)(input.GetTime() / input.GetDuration());

            Vector3 position = Vector3.Lerp(behaviour.StartPosition, behaviour.EndPosition, t);
            blendedPosition += position * weight;
            totalWeight += weight;
            m_useLocalSpace = behaviour.UseLocalSpace;
        }

        if (totalWeight > 0f)
        {
            if (m_useLocalSpace)
                m_target.localPosition = blendedPosition;
            else
                m_target.position = blendedPosition;
        }
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        if (m_originalCaptured && m_target != null)
        {
            m_target.localPosition = m_originalPosition;
        }
    }
}
