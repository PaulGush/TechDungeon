using PlayerObject;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityServiceLocator;

public class WeaponHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponHolder m_weaponHolder;

    [Tooltip("GameObject toggled off when the player is unarmed and back on when they equip a weapon. Leave empty to toggle this HUD's own GameObject.")]
    [SerializeField] private GameObject m_root;

    [Header("Weapon")]
    [SerializeField] private TextMeshProUGUI m_weaponNameText;
    [SerializeField] private Image m_weaponIcon;

    [Header("Ammo")]
    [SerializeField] private TextMeshProUGUI m_ammoNameText;
    [SerializeField] private Image m_ammoIcon;

    [Tooltip("Formatted 'current / max' while the current weapon has a magazine. Shows 'RELOADING...' during a reload. Leave empty to skip.")]
    [SerializeField] private TextMeshProUGUI m_magazineText;

    private AmmoManager m_ammoManager;
    private WeaponShooting m_currentShooting;

    private void Start()
    {
        ServiceLocator.Global.TryGet(out m_ammoManager);

        if (m_ammoManager != null)
        {
            m_ammoManager.OnAmmoChanged += OnAmmoChanged;
            UpdateAmmoDisplay(m_ammoManager.CurrentAmmoSettings);
        }

        if (m_weaponHolder != null)
        {
            m_weaponHolder.OnWeaponChanged += OnWeaponChanged;
            OnWeaponChanged(m_weaponHolder.CurrentWeapon);
        }
        else
        {
            SetRootActive(false);
        }
    }

    private void OnDestroy()
    {
        if (m_ammoManager != null)
            m_ammoManager.OnAmmoChanged -= OnAmmoChanged;
        if (m_weaponHolder != null)
            m_weaponHolder.OnWeaponChanged -= OnWeaponChanged;

        BindShooting(null);
    }

    private void OnAmmoChanged(AmmoSettings settings) => UpdateAmmoDisplay(settings);

    private void OnWeaponChanged(GameObject weapon)
    {
        SetRootActive(weapon != null);

        WeaponShooting shooting = weapon != null ? weapon.GetComponent<WeaponShooting>() : null;
        BindShooting(shooting);
        UpdateWeaponDisplay(weapon, shooting);
    }

    private void SetRootActive(bool active)
    {
        GameObject root = m_root != null ? m_root : gameObject;
        if (root.activeSelf != active)
            root.SetActive(active);
    }

    private void BindShooting(WeaponShooting shooting)
    {
        if (m_currentShooting != null)
        {
            m_currentShooting.OnMagazineChanged -= OnMagazineChanged;
            m_currentShooting.OnReloadStarted -= OnReloadStarted;
            m_currentShooting.OnReloadCompleted -= OnReloadCompleted;
            m_currentShooting.OnReloadCancelled -= OnReloadCancelled;
        }

        m_currentShooting = shooting;

        if (m_currentShooting != null)
        {
            m_currentShooting.OnMagazineChanged += OnMagazineChanged;
            m_currentShooting.OnReloadStarted += OnReloadStarted;
            m_currentShooting.OnReloadCompleted += OnReloadCompleted;
            m_currentShooting.OnReloadCancelled += OnReloadCancelled;
            RefreshMagazineDisplay();
        }
        else if (m_magazineText != null)
        {
            m_magazineText.text = string.Empty;
        }
    }

    private void OnMagazineChanged(int current, int max) => RefreshMagazineDisplay();
    private void OnReloadStarted(float duration) => SetReloadingText();
    private void OnReloadCompleted() => RefreshMagazineDisplay();
    private void OnReloadCancelled() => RefreshMagazineDisplay();

    private void RefreshMagazineDisplay()
    {
        if (m_magazineText == null || m_currentShooting == null) return;
        if (!m_currentShooting.UsesMagazine)
        {
            m_magazineText.text = string.Empty;
            return;
        }
        m_magazineText.text = $"{m_currentShooting.MagazineCurrent} / {m_currentShooting.MagazineMax}";
    }

    private void SetReloadingText()
    {
        if (m_magazineText == null) return;
        m_magazineText.text = "RELOADING...";
    }

    private void UpdateWeaponDisplay(GameObject weapon, WeaponShooting shooting)
    {
        if (weapon == null) return;

        if (m_weaponNameText != null)
        {
            string displayName = shooting != null ? shooting.DisplayName : null;
            m_weaponNameText.text = (string.IsNullOrWhiteSpace(displayName) ? weapon.name : displayName).ToUpper();
        }

        if (m_weaponIcon != null)
        {
            SpriteRenderer sr = weapon.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                m_weaponIcon.sprite = sr.sprite;
                m_weaponIcon.enabled = true;
            }
            else
            {
                m_weaponIcon.enabled = false;
            }
        }
    }

    private void UpdateAmmoDisplay(AmmoSettings settings)
    {
        if (settings == null) return;

        if (m_ammoNameText != null)
            m_ammoNameText.text = settings.DisplayName;

        if (m_ammoIcon != null)
        {
            if (settings.Icon != null)
            {
                m_ammoIcon.sprite = settings.Icon;
                m_ammoIcon.enabled = true;
            }
            else
            {
                m_ammoIcon.enabled = false;
            }
        }
    }
}
