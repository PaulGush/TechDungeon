using TMPro;
using UnityEngine;
using UnityServiceLocator;

public class StatsHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EntityHealth m_health;
    [SerializeField] private TextMeshProUGUI m_armorText;
    [SerializeField] private TextMeshProUGUI m_damageText;
    [SerializeField] private TextMeshProUGUI m_speedText;

    private ItemManager m_itemManager;

    private void Start()
    {
        ServiceLocator.Global.TryGet(out m_itemManager);
        if (m_itemManager != null)
            m_itemManager.OnItemAdded += OnItemAdded;

        m_health.OnHealthChanged += OnHealthChanged;

        UpdateDisplay();
    }

    private void OnDestroy()
    {
        if (m_itemManager != null)
            m_itemManager.OnItemAdded -= OnItemAdded;

        m_health.OnHealthChanged -= OnHealthChanged;
    }

    private void OnItemAdded(Item item) => UpdateDisplay();
    private void OnHealthChanged(int health) => UpdateArmorDisplay();

    private void UpdateDisplay()
    {
        UpdateArmorDisplay();
        UpdateDamageDisplay();
        UpdateSpeedDisplay();
    }

    private void UpdateArmorDisplay()
    {
        if (m_armorText != null)
            m_armorText.text = $"ARM: {m_health.Armor}";
    }

    private void UpdateDamageDisplay()
    {
        if (m_damageText != null && m_itemManager != null)
        {
            int flat = m_itemManager.GetFlatDamageBonus();
            float mult = m_itemManager.GetDamageMultiplier();
            m_damageText.text = flat > 0 || mult > 1f
                ? $"DMG: +{flat} x{mult:F1}"
                : "DMG: --";
        }
    }

    private void UpdateSpeedDisplay()
    {
        if (m_speedText != null && m_itemManager != null)
        {
            float mult = m_itemManager.GetSpeedMultiplier();
            m_speedText.text = mult > 1f
                ? $"SPD: x{mult:F1}"
                : "SPD: --";
        }
    }
}
