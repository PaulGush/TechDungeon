using System.Collections.Generic;
using Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityServiceLocator;

namespace PlayerObject
{
    public class WeaponHolder : MonoBehaviour
    {
        [SerializeField] private Camera m_camera;
        [SerializeField] private InputReader m_inputReader;
        [SerializeField] private EntityHealth m_health;

        private PlayerInteractionDisplay m_interactionDisplay;

        private const float WeaponRotationOffset = -90f;
        private const float WallBuffer = 0.05f;
        // Squared deadzone for gamepad/stick look input — below this magnitude we fall back to the mouse cursor.
        private const float LookInputDeadzoneSqr = 0.1f;

        private Vector2 m_previousFrameMousePosition;
        private RoomManager m_roomManager;
        private WeaponShooting m_currentWeaponShooting;
        private float m_weaponOriginalLocalY;
        private float m_shootPointLocalOffset;

        private RoomManager RoomManager => m_roomManager ??= ServiceLocator.Global.Get<RoomManager>();

        private void LateUpdate()
        {
            if (m_health.IsDead)
                return;

            HandleWeaponPositionAndRotation();
            ClampWeaponToWalls();
        }

        private void OnDestroy()
        {
            m_inputReader.Interact -= Equip;
        }

        private void HandleWeaponPositionAndRotation()
        {
            Vector2 lookDirection = m_inputReader.LookDirection;
            if (lookDirection.sqrMagnitude > LookInputDeadzoneSqr)
            {
                transform.rotation = MathUtilities.CalculateAimRotation(lookDirection, WeaponRotationOffset);
                return;
            }

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            if (mousePosition != m_previousFrameMousePosition)
            {
                Vector3 diff = m_camera.ScreenToWorldPoint(mousePosition) - transform.position;
                diff.Normalize();
                transform.rotation = MathUtilities.CalculateAimRotation(diff, WeaponRotationOffset);
            }
            m_previousFrameMousePosition = mousePosition;
        }

        private void ClampWeaponToWalls()
        {
            if (m_currentWeapon == null || m_currentWeaponShooting == null) return;

            // Total reach from holder to shoot point tip
            float totalReach = m_weaponOriginalLocalY + m_shootPointLocalOffset;

            Vector2 origin = transform.position;
            Vector2 aimDir = (Vector2)transform.up;

            RaycastHit2D hit = Physics2D.Raycast(origin, aimDir, totalReach, GameConstants.Layers.WallsLayerMask);

            if (hit)
            {
                // Pull weapon back so the shoot point stops at the wall (minus buffer)
                float clampedLocalY = Mathf.Max(0f, hit.distance - m_shootPointLocalOffset - WallBuffer);
                Vector3 localPos = m_currentWeapon.transform.localPosition;
                localPos.y = clampedLocalY;
                m_currentWeapon.transform.localPosition = localPos;
            }
            else
            {
                // Restore original position
                Vector3 localPos = m_currentWeapon.transform.localPosition;
                localPos.y = m_weaponOriginalLocalY;
                m_currentWeapon.transform.localPosition = localPos;
            }
        }

        private GameObject m_currentWeapon;
        private GameObject m_weaponCandidate;
        private readonly List<GameObject> m_weaponsInRange = new();

        public System.Action<GameObject> OnWeaponChanged;
        public GameObject CurrentWeapon => m_currentWeapon;

        public void Reset()
        {
            if (m_currentWeapon != null)
            {
                foreach (Component component in m_currentWeapon.GetComponents(typeof(IWeapon)))
                {
                    ((IWeapon)component).Unequip();
                }

                Destroy(m_currentWeapon);
                m_currentWeapon = null;
                m_currentWeaponShooting = null;
            }

            m_weaponCandidate = null;
            m_weaponsInRange.Clear();
            m_interactionDisplay?.Hide(this);
            m_inputReader.Interact -= Equip;
            OnWeaponChanged?.Invoke(null);
        }

        private void Equip()
        {
            if (m_currentWeapon != null)
            {
                Unequip();
            }

            m_currentWeapon = m_weaponCandidate;
            m_currentWeapon.transform.SetParent(transform);

            m_weaponsInRange.Remove(m_currentWeapon);

            foreach (Component component in m_currentWeapon.GetComponents(typeof(IWeapon)))
            {
                IWeapon weaponComponent = (IWeapon)component;
                weaponComponent.Equip();
            }

            m_currentWeaponShooting = m_currentWeapon.GetComponent<WeaponShooting>();

            // Cache the weapon's default local Y offset and shoot point distance from weapon pivot
            m_weaponOriginalLocalY = m_currentWeapon.transform.localPosition.y;
            Vector2 shootWorld = m_currentWeaponShooting.ShootPointPosition;
            Vector2 weaponWorld = m_currentWeapon.transform.position;
            m_shootPointLocalOffset = Vector2.Distance(shootWorld, weaponWorld);

            OnWeaponChanged?.Invoke(m_currentWeapon);
            UpdateWeaponCandidate();
        }

        private void Unequip()
        {
            // Restore original position before unparenting
            Vector3 localPos = m_currentWeapon.transform.localPosition;
            localPos.y = m_weaponOriginalLocalY;
            m_currentWeapon.transform.localPosition = localPos;

            m_currentWeapon.transform.SetParent(RoomManager.CurrentRoomTransform);

            foreach (Component component in m_currentWeapon.GetComponents(typeof(IWeapon)))
            {
                IWeapon weaponComponent = (IWeapon)component;
                weaponComponent.Unequip();
            }

            m_currentWeaponShooting = null;
            m_currentWeapon = null;
            OnWeaponChanged?.Invoke(null);
        }


        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.layer != GameConstants.Layers.WeaponLayer) return;

            other.TryGetComponent(out Weapon weapon);

            if (weapon != null && weapon.IsSpawning) return;

            if (!m_weaponsInRange.Contains(other.gameObject))
            {
                m_weaponsInRange.Add(other.gameObject);

                Lootable lootable = other.GetComponent<Lootable>();
                if (lootable != null && lootable.OnCollected != null)
                {
                    lootable.OnCollected.Invoke();
                    lootable.OnCollected = null;
                }
            }

            if (weapon != null)
            {
                weapon.SetWeaponHolder(this);
            }

            UpdateWeaponCandidate();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.gameObject.layer != GameConstants.Layers.WeaponLayer) return;

            m_weaponsInRange.Remove(other.gameObject);
            UpdateWeaponCandidate();
        }

        private void UpdateWeaponCandidate()
        {
            m_weaponsInRange.RemoveAll(w => w == null);

            m_interactionDisplay ??= ServiceLocator.Global.Get<PlayerInteractionDisplay>();

            if (m_weaponsInRange.Count == 0)
            {
                if (m_weaponCandidate != null)
                {
                    m_weaponCandidate = null;
                    m_interactionDisplay?.Hide(this);
                    m_inputReader.Interact -= Equip;
                }
                return;
            }

            GameObject closest = null;
            float closestDist = float.MaxValue;
            foreach (GameObject weapon in m_weaponsInRange)
            {
                float dist = (weapon.transform.position - transform.position).sqrMagnitude;
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = weapon;
                }
            }

            bool hadNoCandidate = m_weaponCandidate == null;
            m_weaponCandidate = closest;

            if (hadNoCandidate)
            {
                m_inputReader.Interact += Equip;
            }

            string text = m_currentWeapon != null
                ? "Press E to swap " + m_currentWeapon.name + " for " + m_weaponCandidate.name
                : "Press E to equip " + m_weaponCandidate.name;
            m_interactionDisplay?.Show(text, this);
        }
    }
}
