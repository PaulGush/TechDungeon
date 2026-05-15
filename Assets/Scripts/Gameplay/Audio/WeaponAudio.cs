using UnityEngine;
using UnityServiceLocator;

namespace Gameplay.Audio
{
    [RequireComponent(typeof(WeaponShooting))]
    public class WeaponAudio : MonoBehaviour
    {
        [Tooltip("SoundEvent played every time the weapon fires (after obstruction/ammo checks).")]
        [SerializeField] private SoundEvent m_fireSound;

        [Tooltip("Optional additional SoundEvent layered on top of the base fire sound when non-standard ammo is consumed (explosive pop, chain crackle, etc.). Leave empty to skip.")]
        [SerializeField] private SoundEvent m_ammoLayerSound;

        private WeaponShooting m_shooting;
        private AudioService m_audio;

        private void Awake()
        {
            m_shooting = GetComponent<WeaponShooting>();
        }

        private void OnEnable()
        {
            if (m_shooting != null)
                m_shooting.OnFired += HandleFired;
        }

        private void OnDisable()
        {
            if (m_shooting != null)
                m_shooting.OnFired -= HandleFired;
        }

        private void HandleFired(AmmoSettings ammoSettings)
        {
            if (m_audio == null && !ServiceLocator.Global.TryGet(out m_audio))
                return;

            if (m_fireSound != null)
                m_audio.PlayAttached(m_fireSound, transform);

            if (ammoSettings != null && m_ammoLayerSound != null)
                m_audio.PlayAttached(m_ammoLayerSound, transform);
        }
    }
}
