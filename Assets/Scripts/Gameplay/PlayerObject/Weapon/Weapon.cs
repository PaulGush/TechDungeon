using UnityEngine;

namespace PlayerObject
{
    /// <summary>
    /// Represents a weapon that can be dropped and picked up in the world.
    /// Inherits from Lootable to reuse drop/bounce/rarity behavior for weapon items on the ground.
    /// When equipped, bounce is disabled and the weapon attaches to the player's WeaponHolder.
    /// </summary>
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
            transform.localScale = Vector3.one;
            transform.SetPositionAndRotation(transform.position, Quaternion.identity);

            m_isEquipped = false;
            m_bounceEnabled = true;
            SetBounceTargets();
        }

        private void FixedUpdate()
        {
            if (!m_isEquipped) return;
            bool shouldFlip = m_weaponHolder.transform.rotation.eulerAngles.z < 180;
            transform.localScale = new Vector3(1f, shouldFlip ? -1f : 1f, 1f);
        }
    }
}