using UnityEngine;
using UnityServiceLocator;

namespace Gameplay.Audio
{
    [RequireComponent(typeof(Projectile))]
    public class ProjectileAudio : MonoBehaviour
    {
        [Tooltip("Played on the final damage hit against an EntityHealth.")]
        [SerializeField] private SoundEvent m_entityImpactSound;

        [Tooltip("Played when the projectile hits a destroy-layer surface (walls, obstacles).")]
        [SerializeField] private SoundEvent m_wallImpactSound;

        private Projectile m_projectile;
        private AudioService m_audio;

        private void Awake()
        {
            m_projectile = GetComponent<Projectile>();
        }

        private void OnEnable()
        {
            if (m_projectile == null) return;
            m_projectile.OnEntityImpact += HandleEntityImpact;
            m_projectile.OnWallImpact += HandleWallImpact;
        }

        private void OnDisable()
        {
            if (m_projectile == null) return;
            m_projectile.OnEntityImpact -= HandleEntityImpact;
            m_projectile.OnWallImpact -= HandleWallImpact;
        }

        private void HandleEntityImpact() => Play(m_entityImpactSound);
        private void HandleWallImpact() => Play(m_wallImpactSound);

        private void Play(SoundEvent ev)
        {
            if (ev == null) return;
            if (m_audio == null && !ServiceLocator.Global.TryGet(out m_audio))
                return;
            m_audio.PlayAt(ev, transform.position);
        }
    }
}
