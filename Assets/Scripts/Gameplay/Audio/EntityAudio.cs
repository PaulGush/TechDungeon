using UnityEngine;
using UnityServiceLocator;

namespace Gameplay.Audio
{
    public class EntityAudio : MonoBehaviour
    {
        [SerializeField] private EntityHealth m_health;

        [Tooltip("Played whenever the entity takes damage (non-lethal and lethal both fire OnTakeDamage — death plays on top).")]
        [SerializeField] private SoundEvent m_hurtSound;

        [Tooltip("Played whenever the entity heals.")]
        [SerializeField] private SoundEvent m_healSound;

        [Tooltip("Played when the entity dies (OnDeath).")]
        [SerializeField] private SoundEvent m_deathSound;

        private AudioService m_audio;

        private void OnEnable()
        {
            if (m_health == null) return;
            m_health.OnTakeDamage += HandleHurt;
            m_health.OnHeal += HandleHeal;
            m_health.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            if (m_health == null) return;
            m_health.OnTakeDamage -= HandleHurt;
            m_health.OnHeal -= HandleHeal;
            m_health.OnDeath -= HandleDeath;
        }

        private void HandleHurt() => Play(m_hurtSound);
        private void HandleHeal() => Play(m_healSound);
        private void HandleDeath() => Play(m_deathSound);

        private void Play(SoundEvent ev)
        {
            if (ev == null) return;
            if (m_audio == null && !ServiceLocator.Global.TryGet(out m_audio))
                return;
            m_audio.PlayAt(ev, transform.position);
        }
    }
}
