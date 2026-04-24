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

    [Header("Ammo")]
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

    private void UpdateAmmoDisplay(AmmoSettings settings)
    {
        if (settings == null) return;

        if (m_ammoIcon != null)
        {
            m_ammoIcon.color = settings.ProjectileColor;
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
