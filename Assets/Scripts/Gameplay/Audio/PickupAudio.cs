using UnityEngine;
using UnityServiceLocator;

namespace Gameplay.Audio
{
    [RequireComponent(typeof(Pickup))]
    public class PickupAudio : MonoBehaviour
    {
        [SerializeField] private SoundEvent m_pickupSound;

        private Pickup m_pickup;
        private AudioService m_audio;

        private void Awake()
        {
            m_pickup = GetComponent<Pickup>();
        }

        private void OnEnable()
        {
            if (m_pickup != null)
                m_pickup.OnPickedUp += HandlePickedUp;
        }

        private void OnDisable()
        {
            if (m_pickup != null)
                m_pickup.OnPickedUp -= HandlePickedUp;
        }

        private void HandlePickedUp()
        {
            if (m_pickupSound == null) return;
            if (m_audio == null && !ServiceLocator.Global.TryGet(out m_audio))
                return;
            m_audio.PlayAt(m_pickupSound, transform.position);
        }
    }
}
