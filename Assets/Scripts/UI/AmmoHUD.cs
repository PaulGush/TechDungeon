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

    private AmmoManager m_ammoManager;

    private void Start()
    {
        ServiceLocator.Global.TryGet(out m_ammoManager);
        if (m_ammoManager == null) return;

        m_ammoManager.OnAmmoChanged += OnAmmoChanged;
        m_ammoManager.OnAmmoCountChanged += OnAmmoCountChanged;

        UpdateDisplay(m_ammoManager.CurrentAmmoSettings);
    }

    private void OnDestroy()
    {
        if (m_ammoManager != null)
        {
            m_ammoManager.OnAmmoChanged -= OnAmmoChanged;
            m_ammoManager.OnAmmoCountChanged -= OnAmmoCountChanged;
        }
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
