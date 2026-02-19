using UnityEngine;

namespace PlayerObject
{
    public class Weapon : Lootable, IWeapon
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer m_spriteRenderer;
        [SerializeField] private WeaponHolder m_weaponHolder;

        [Header("Settings")] 
        [SerializeField] private float m_distanceFromPlayer = 0.65f;
        [SerializeField] private float m_zRotationOffset = 90f;

        private bool m_isEquipped = false;

        public void SetWeaponHolder(WeaponHolder weaponHolder) => m_weaponHolder = weaponHolder;

        public void Equip()
        {
            m_weaponHolder = GetComponentInParent<WeaponHolder>();
            transform.localPosition = new Vector3(0, m_distanceFromPlayer, 0);
            transform.localEulerAngles = new Vector3(0, 0, m_zRotationOffset);

            m_isEquipped = true;
            m_bounceEnabled = false;
        }

        public void Unequip()
        {
            Quaternion unequipRotation = Quaternion.identity;
            if (m_spriteRenderer.flipY)
            {
                unequipRotation = Quaternion.Euler(0, 0, 180);
            }

            transform.SetPositionAndRotation(transform.position, unequipRotation);
            
            m_isEquipped = false;
            m_bounceEnabled = true;
            SetBounceTargets();
        }

        private void FixedUpdate()
        {
            if (!m_isEquipped) return;
            m_spriteRenderer.flipY = m_weaponHolder.transform.rotation.eulerAngles.z < 180;    
        }
    }
}