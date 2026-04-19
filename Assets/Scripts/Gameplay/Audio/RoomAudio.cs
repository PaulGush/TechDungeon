using UnityEngine;
using UnityServiceLocator;

namespace Gameplay.Audio
{
    /// <summary>
    /// 2D stinger emitter for room lifecycle events. Place one instance in the scene
    /// (e.g., on the AudioService GameObject). Resolves RoomManager from the global
    /// service locator and fires on room load / room cleared.
    /// </summary>
    public class RoomAudio : MonoBehaviour
    {
        [Tooltip("Played whenever any room finishes loading. Leave empty to skip a generic room-enter cue.")]
        [SerializeField] private SoundEvent m_roomEnterSound;

        [Tooltip("Played when a combat room is cleared of enemies.")]
        [SerializeField] private SoundEvent m_roomClearedSound;

        private RoomManager m_roomManager;
        private AudioService m_audio;

        private void OnEnable()
        {
            ServiceLocator.Global.TryGet(out m_roomManager);
            if (m_roomManager == null) return;

            m_roomManager.OnRoomLoaded += HandleRoomLoaded;
            m_roomManager.OnRoomCleared += HandleRoomCleared;
        }

        private void OnDisable()
        {
            if (m_roomManager == null) return;
            m_roomManager.OnRoomLoaded -= HandleRoomLoaded;
            m_roomManager.OnRoomCleared -= HandleRoomCleared;
        }

        private void HandleRoomLoaded(RoomSettings _) => Play(m_roomEnterSound);
        private void HandleRoomCleared() => Play(m_roomClearedSound);

        private void Play(SoundEvent ev)
        {
            if (ev == null) return;
            if (m_audio == null && !ServiceLocator.Global.TryGet(out m_audio))
                return;
            m_audio.Play(ev);
        }
    }
}
