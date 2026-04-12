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

    private MutationManager m_mutationManager;

    private void Start()
    {
        ServiceLocator.Global.TryGet(out m_mutationManager);
        if (m_mutationManager != null)
            m_mutationManager.OnMutationAdded += OnMutationAdded;

        m_health.OnHealthChanged += OnHealthChanged;

        UpdateDisplay();
    }

    private void OnDestroy()
    {
        if (m_mutationManager != null)
            m_mutationManager.OnMutationAdded -= OnMutationAdded;

        m_health.OnHealthChanged -= OnHealthChanged;
    }

    private void OnMutationAdded(Mutation mutation) => UpdateDisplay();
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
        if (m_damageText != null && m_mutationManager != null)
        {
            int flat = m_mutationManager.GetFlatDamageBonus();
            float mult = m_mutationManager.GetDamageMultiplier();
            m_damageText.text = flat > 0 || mult > 1f
                ? $"DMG: +{flat} x{mult:F1}"
                : "DMG: --";
        }
    }

    private void UpdateSpeedDisplay()
    {
        if (m_speedText != null && m_mutationManager != null)
        {
            float mult = m_mutationManager.GetSpeedMultiplier();
            m_speedText.text = mult > 1f
                ? $"SPD: x{mult:F1}"
                : "SPD: --";
        }
    }
}
