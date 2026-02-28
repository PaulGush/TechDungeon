using Input;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerObject
{
    public class WeaponHolder : MonoBehaviour
    {
        [SerializeField] private Camera m_camera;
        [SerializeField] private InputReader m_inputReader;
        [SerializeField] private TextMeshPro m_interactText;
        [SerializeField] private Transform m_environmentParent;

        private const float WeaponRotationOffset = -90f;
        private Vector2 m_previousFrameMousePosition;

        private void FixedUpdate()
        {
            HandleWeaponPositionAndRotation();
        }

        private void HandleWeaponPositionAndRotation()
        {
            Vector2 lookDirection = m_inputReader.LookDirection;
            if (lookDirection.sqrMagnitude > 0.1f)
            {
                transform.rotation = MathUtilities.CalculateAimRotation(lookDirection, WeaponRotationOffset);
            }
            else if (m_previousFrameMousePosition != Mouse.current.position.ReadValue())
            {
                Vector3 diff = m_camera.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - transform.position;
                diff.Normalize();
                transform.rotation = MathUtilities.CalculateAimRotation(diff, WeaponRotationOffset);
            }
            m_previousFrameMousePosition = Mouse.current.position.ReadValue();
        }

        private GameObject m_currentWeapon;
        private GameObject m_weaponCandidate;

        private void Equip()
        {
            if (m_currentWeapon != null)
            {
                Unequip();
            }

            m_currentWeapon = m_weaponCandidate;
            m_currentWeapon.transform.SetParent(transform);

            foreach (Component component in m_currentWeapon.GetComponents(typeof(IWeapon)))
            {
                IWeapon weaponComponent = (IWeapon)component;
                weaponComponent.Equip();
            }
        }

        private void Unequip()
        {
            m_currentWeapon.transform.SetParent(m_environmentParent);

            foreach (Component component in m_currentWeapon.GetComponents(typeof(IWeapon)))
            {
                IWeapon weaponComponent = (IWeapon)component;
                weaponComponent.Unequip();
            }

            m_currentWeapon = null;
        }


        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.layer != GameConstants.Layers.WeaponLayer) return;

            other.TryGetComponent(out Weapon weaponVisual);

            m_weaponCandidate = other.gameObject;

            if (weaponVisual != null)
            {
                weaponVisual.SetWeaponHolder(this);
            }

            m_interactText.enabled = true;
            m_interactText.text = m_currentWeapon != null
                ? "Press E to swap " + m_currentWeapon.name + " for " + m_weaponCandidate.name
                : "Press E to equip " + other.gameObject.name;

            m_inputReader.Interact += Equip;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.gameObject.layer != GameConstants.Layers.WeaponLayer) return;

            m_weaponCandidate = null;

            m_interactText.enabled = false;
            m_inputReader.Interact -= Equip;
        }
    }
}
