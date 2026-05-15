using System.Collections.Generic;
using UnityEngine;
using UnityServiceLocator;

namespace Gameplay.Audio
{
    [RequireComponent(typeof(Chest))]
    public class ChestAudio : MonoBehaviour
    {
        [Tooltip("Played when the chest is opened (lid unlock / creak).")]
        [SerializeField] private SoundEvent m_openSound;

        [Tooltip("Played when every item in the chest has been collected (empty chime).")]
        [SerializeField] private SoundEvent m_emptiedSound;

        private Chest m_chest;
        private AudioService m_audio;

        private void Awake()
        {
            m_chest = GetComponent<Chest>();
        }

        private void OnEnable()
        {
            if (m_chest == null) return;
            m_chest.OnChestOpened += HandleOpened;
            m_chest.OnAllItemsCollected += HandleEmptied;
        }

        private void OnDisable()
        {
            if (m_chest == null) return;
            m_chest.OnChestOpened -= HandleOpened;
            m_chest.OnAllItemsCollected -= HandleEmptied;
        }

        private void HandleOpened(List<GameObject> _) => Play(m_openSound);
        private void HandleEmptied() => Play(m_emptiedSound);

        private void Play(SoundEvent ev)
        {
            if (ev == null) return;
            if (m_audio == null && !ServiceLocator.Global.TryGet(out m_audio))
                return;
            m_audio.PlayAt(ev, transform.position);
        }
    }
}
