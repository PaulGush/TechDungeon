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
        [Tooltip("Optional. If set, the stats panel is instantiated from this prefab so its look can be configured in the prefab. If null, a procedural panel is built at runtime.")]
        [SerializeField] private WeaponStatsPanel m_statsPanelPrefab;

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
        private Quaternion m_baseAimRotation = Quaternion.identity;

        private RoomManager RoomManager => m_roomManager ??= ServiceLocator.Global.Get<RoomManager>();

        private void Awake()
        {
            ServiceLocator.Global.Register(this);
            // The "show pickup details" toggle is game-wide; own the input subscription here (the
            // holder is always on the player) and just react to the preference changing.
            m_inputReader.ViewWeaponStats += PickupDetailsPreference.Toggle;
            PickupDetailsPreference.Changed += RefreshStatsPanel;
        }

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
            m_inputReader.ViewWeaponStats -= PickupDetailsPreference.Toggle;
            PickupDetailsPreference.Changed -= RefreshStatsPanel;
            if (m_statsPanel != null) Destroy(m_statsPanel.gameObject);
        }

        private void HandleWeaponPositionAndRotation()
        {
            Vector2 lookDirection = m_inputReader.LookDirection;
            if (lookDirection.sqrMagnitude > LookInputDeadzoneSqr)
            {
                m_baseAimRotation = MathUtilities.CalculateAimRotation(lookDirection, WeaponRotationOffset);
            }
            else
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                if (mousePosition != m_previousFrameMousePosition)
                {
                    Vector3 diff = m_camera.ScreenToWorldPoint(mousePosition) - transform.position;
                    diff.Normalize();
                    m_baseAimRotation = MathUtilities.CalculateAimRotation(diff, WeaponRotationOffset);
                }
                m_previousFrameMousePosition = mousePosition;
            }

            // Layer recoil on top each frame so the muzzle climb decays back to zero even when the
            // player isn't moving the mouse or stick. Sign matches the sprite-flip condition in
            // Weapon.FixedUpdate so the tilt always points toward the sprite's "up" side.
            float recoilDeg = m_currentWeaponShooting != null ? m_currentWeaponShooting.CurrentRecoilDegrees : 0f;
            if (recoilDeg > 0f)
            {
                float sign = m_baseAimRotation.eulerAngles.z < 180f ? -1f : 1f;
                transform.rotation = m_baseAimRotation * Quaternion.Euler(0f, 0f, recoilDeg * sign);
            }
            else
            {
                transform.rotation = m_baseAimRotation;
            }
        }

        private void ClampWeaponToWalls()
        {
            if (m_currentWeapon == null || m_currentWeaponShooting == null) return;

            // Total reach from holder to shoot point tip
            float totalReach = m_weaponOriginalLocalY + m_shootPointLocalOffset;

            Vector2 origin = transform.position;
            Vector2 aimDir = (Vector2)transform.up;

            RaycastHit2D hit = Physics2D.Raycast(origin, aimDir, totalReach, GameConstants.Layers.WallsLayerMask);

            float kickback = m_currentWeaponShooting.CurrentKickbackOffset;
            Vector3 localPos = m_currentWeapon.transform.localPosition;

            if (hit)
            {
                // Pull weapon back so the shoot point stops at the wall (minus buffer)
                float clampedLocalY = Mathf.Max(0f, hit.distance - m_shootPointLocalOffset - WallBuffer);
                localPos.y = Mathf.Max(0f, clampedLocalY + kickback);
            }
            else
            {
                // Restore original position, plus any active kickback offset
                localPos.y = Mathf.Max(0f, m_weaponOriginalLocalY + kickback);
            }

            m_currentWeapon.transform.localPosition = localPos;
        }

        private GameObject m_currentWeapon;
        private GameObject m_weaponCandidate;
        private readonly List<GameObject> m_weaponsInRange = new();

        private WeaponStatsPanel m_statsPanel;

        public System.Action<GameObject> OnWeaponChanged;
        public GameObject CurrentWeapon => m_currentWeapon;

        /// <summary>
        /// World position of the equipped weapon's shoot point, or null if no weapon is equipped.
        /// Lets non-shooting systems (e.g. abilities) originate visuals from the muzzle.
        /// </summary>
        public Vector2? CurrentShootPoint =>
            m_currentWeaponShooting != null ? m_currentWeaponShooting.ShootPointPosition : (Vector2?)null;

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
            RefreshStatsPanel();
            OnWeaponChanged?.Invoke(null);
        }

        private void Equip()
        {
            if (m_weaponCandidate == null) return;
            if (m_interactionDisplay != null && m_interactionDisplay.CurrentSource != this) return;

            if (m_currentWeapon != null)
            {
                Unequip();
            }

            m_currentWeapon = m_weaponCandidate;
            m_currentWeapon.transform.SetParent(transform);

            m_weaponsInRange.Remove(m_currentWeapon);

            // Notify any spawner (e.g. RoomRewardChest) that this weapon is now picked up. With the
            // interact-prompt rework, "in range" no longer counts — equipping is the collection.
            Lootable lootable = m_currentWeapon.GetComponent<Lootable>();
            if (lootable != null && lootable.OnCollected != null)
            {
                lootable.OnCollected.Invoke();
                lootable.OnCollected = null;
            }

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
                    RefreshStatsPanel();
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
                ? "[Interact] swap " + GetWeaponDisplayName(m_currentWeapon) + " for " + GetWeaponDisplayName(m_weaponCandidate)
                : "[Interact] equip " + GetWeaponDisplayName(m_weaponCandidate);
            // Held weapon → comparison panel; no held weapon → just the candidate's stats.
            text += m_currentWeapon != null ? "   [Sprint] compare" : "   [Sprint] details";
            m_interactionDisplay?.Show(text, this);

            RefreshStatsPanel();
        }

        // Show the stats panel (comparing the candidate against the held weapon) iff the player has
        // the "show details" preference on and there's a weapon candidate in range; otherwise hide it.
        // Called when the candidate changes or the preference toggles.
        private void RefreshStatsPanel()
        {
            if (PickupDetailsPreference.ShowDetails && m_weaponCandidate != null)
            {
                if (m_statsPanel == null)
                {
                    if (m_statsPanelPrefab != null)
                    {
                        m_statsPanel = Instantiate(m_statsPanelPrefab);
                        m_statsPanel.gameObject.name = "WeaponStatsPanel";
                    }
                    else
                    {
                        var go = new GameObject("WeaponStatsPanel", typeof(RectTransform), typeof(Canvas), typeof(WeaponStatsPanel));
                        m_statsPanel = go.GetComponent<WeaponStatsPanel>();
                    }
                }
                m_statsPanel.Show(m_weaponCandidate.GetComponent<WeaponShooting>(), m_currentWeaponShooting);
            }
            else
            {
                m_statsPanel?.Hide();
            }
        }

        private static string GetWeaponDisplayName(GameObject weapon)
        {
            WeaponShooting shooting = weapon.GetComponent<WeaponShooting>();
            string displayName = shooting != null ? shooting.DisplayName : null;
            return string.IsNullOrWhiteSpace(displayName) ? weapon.name : displayName;
        }
    }
}
