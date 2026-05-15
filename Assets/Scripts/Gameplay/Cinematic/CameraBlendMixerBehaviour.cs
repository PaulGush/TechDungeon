using Unity.Cinemachine;
using UnityEngine.Playables;

public class CameraBlendMixerBehaviour : PlayableBehaviour
{
    private CinemachineBrain m_brain;
    private CinemachineBlendDefinition m_originalBlend;
    private bool m_originalCaptured;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        m_brain = playerData as CinemachineBrain;
        if (m_brain == null) return;

        if (!m_originalCaptured)
        {
            m_originalBlend = m_brain.DefaultBlend;
            m_originalCaptured = true;
        }

        int inputCount = playable.GetInputCount();

        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            if (weight <= 0f) continue;

            var input = (ScriptPlayable<CameraBlendBehaviour>)playable.GetInput(i);
            var behaviour = input.GetBehaviour();

            m_brain.DefaultBlend = new CinemachineBlendDefinition(
                behaviour.BlendStyle, behaviour.BlendDuration);
            return;
        }
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        if (m_originalCaptured && m_brain != null)
        {
            m_brain.DefaultBlend = m_originalBlend;
        }
    }
}
