using UnityEngine.Playables;

public class GameStateMixerBehaviour : PlayableBehaviour
{
    private RoomManager m_roomManager;
    private bool m_originalCaptured;
    private bool m_originalPlayerInput;
    private bool m_originalGodMode;
    private bool m_originalConfiner;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        m_roomManager = playerData as RoomManager;
        if (m_roomManager == null) return;

        if (!m_originalCaptured)
        {
            m_originalPlayerInput = m_roomManager.IsPlayerInputActive;
            m_originalGodMode = m_roomManager.IsGodModeActive;
            m_originalConfiner = m_roomManager.IsCameraConfinerActive;
            m_originalCaptured = true;
        }

        int inputCount = playable.GetInputCount();

        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            if (weight <= 0f) continue;

            var input = (ScriptPlayable<GameStateBehaviour>)playable.GetInput(i);
            var behaviour = input.GetBehaviour();

            m_roomManager.SetPlayerInputActive(behaviour.PlayerInputActive);
            m_roomManager.SetPlayerGodMode(behaviour.PlayerGodMode);
            m_roomManager.SetCameraConfinerActive(behaviour.CameraConfinerActive);
            break; // First active clip wins (no blending for booleans).
        }
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        if (m_originalCaptured && m_roomManager != null)
        {
            m_roomManager.SetPlayerInputActive(m_originalPlayerInput);
            m_roomManager.SetPlayerGodMode(m_originalGodMode);
            m_roomManager.SetCameraConfinerActive(m_originalConfiner);
        }
    }
}
