using PlayerObject;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityServiceLocator;

public class AmmoHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI m_ammoNameText;
    [SerializeField] private TextMeshProUGUI m_ammoCountText;
    [SerializeField] private Image m_ammoIcon;

    [Tooltip("Placeholder text for the current weapon's magazine, formatted as 'current / max'. Shows 'RELOADING...' during a reload. Leave empty to skip.")]
    [SerializeField] private TextMeshProUGUI m_magazineText;

    private AmmoManager m_ammoManager;
    private WeaponHolder m_weaponHolder;
    private WeaponShooting m_currentShooting;

    private void Start()
    {
        ServiceLocator.Global.TryGet(out m_ammoManager);
        ServiceLocator.Global.TryGet(out m_weaponHolder);

        if (m_ammoManager != null)
        {
            m_ammoManager.OnAmmoChanged += OnAmmoChanged;
            m_ammoManager.OnAmmoCountChanged += OnAmmoCountChanged;
            UpdateDisplay(m_ammoManager.CurrentAmmoSettings);
        }

        if (m_weaponHolder != null)
        {
            m_weaponHolder.OnWeaponChanged += OnWeaponChanged;
            OnWeaponChanged(m_weaponHolder.CurrentWeapon);
        }
    }

    private void OnDestroy()
    {
        if (m_ammoManager != null)
        {
            m_ammoManager.OnAmmoChanged -= OnAmmoChanged;
            m_ammoManager.OnAmmoCountChanged -= OnAmmoCountChanged;
        }
        if (m_weaponHolder != null)
            m_weaponHolder.OnWeaponChanged -= OnWeaponChanged;

        BindShooting(null);
    }

    private void OnAmmoChanged(AmmoSettings settings)
    {
        UpdateDisplay(settings);
    }

    private void OnAmmoCountChanged(AmmoType type, int count)
    {
        if (m_ammoManager.CurrentAmmoSettings != null && m_ammoManager.CurrentAmmoSettings.Type == type)
            UpdateCountDisplay(type, count);
    }

    private void OnWeaponChanged(GameObject weapon)
    {
        WeaponShooting shooting = weapon != null ? weapon.GetComponent<WeaponShooting>() : null;
        BindShooting(shooting);
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

    private void UpdateDisplay(AmmoSettings settings)
    {
        if (settings == null) return;

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

        UpdateCountDisplay(settings.Type, m_ammoManager.GetAmmoCount(settings.Type));
    }

    private void UpdateCountDisplay(AmmoType type, int count)
    {
        m_ammoCountText.text = type == AmmoType.Standard ? "INF" : count.ToString();
    }
}
