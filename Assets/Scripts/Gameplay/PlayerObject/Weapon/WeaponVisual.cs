using System;
using UnityEngine;

namespace PlayerObject
{
    public class WeaponVisual : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer m_spriteRenderer;
        [SerializeField] private WeaponAnchor m_weaponAnchor;

        [Header("Settings")] 
        [SerializeField] private float m_distanceFromPlayer = 0.65f;
        [SerializeField] private float m_zRotationOffset = 90f;

        private void Start()
        {
            m_weaponAnchor = GetComponentInParent<WeaponAnchor>();
            transform.localPosition = new Vector3(0, m_distanceFromPlayer, 0);
            transform.localEulerAngles = new Vector3(0, 0, m_zRotationOffset);
        }

        private void FixedUpdate()
        {
            m_spriteRenderer.flipY = m_weaponAnchor.transform.rotation.eulerAngles.z < 180;    
        }
    }
}
