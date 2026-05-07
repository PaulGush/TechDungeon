using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityServiceLocator;

public class AbilityHudSlot : MonoBehaviour
{
    [Header("Slot")]
    [Tooltip("Which slot this HUD element represents (0-3). Matches the input slot — 0=Key 1/DPad Up, 1=Key 2/DPad Right, 2=Key 3/DPad Down, 3=Key 4/DPad Left.")]
    [Range(0, 3)]
    [SerializeField] private int m_slotIndex;

    [Header("References")]
    [Tooltip("GameObject toggled off when no ability is equipped in this slot. Leave empty to toggle this HUD's own GameObject.")]
    [SerializeField] private GameObject m_root;

    [SerializeField] private Image m_iconImage;

    [Tooltip("Image with FillMethod = Radial360. Sweeps back from 0 to 1 as cooldown drains so the ability lights up when ready.")]
    [SerializeField] private Image m_cooldownFill;

    [Tooltip("Optional key prompt label. Leave empty to skip.")]
    [SerializeField] private TextMeshProUGUI m_keyPrompt;

    [Tooltip("Optional Animator on the slot. Receives a 'Press' trigger each time an ability fires — drives the bulge animation. Leave empty to skip.")]
    [SerializeField] private Animator m_animator;

    [Tooltip("Trigger parameter on the animator fired when the ability is used.")]
    [SerializeField] private string m_pressTriggerName = "Press";

    [Tooltip("Tint applied to the icon underneath when the ability is on cooldown — the dimmed 'unavailable' state. Stays applied while the bright top sprite radial-fills back to ready.")]
    [SerializeField] private Color m_dimmedIconColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    private AbilityController m_controller;

    private void Start()
    {
        ServiceLocator.Global.TryGet(out m_controller);
        if (m_controller == null)
        {
            SetRootActive(false);
            return;
        }

        m_controller.OnAbilityEquipped += OnAbilityEquipped;
        m_controller.OnAbilityUsed += OnAbilityUsed;
        OnAbilityEquipped(m_slotIndex, m_controller.GetAbility(m_slotIndex));
    }

    private void OnDestroy()
    {
        if (m_controller != null)
        {
            m_controller.OnAbilityEquipped -= OnAbilityEquipped;
            m_controller.OnAbilityUsed -= OnAbilityUsed;
        }
    }

    private void OnAbilityUsed(int slotIndex)
    {
        if (slotIndex != m_slotIndex) return;
        if (m_animator != null && !string.IsNullOrEmpty(m_pressTriggerName))
            m_animator.SetTrigger(m_pressTriggerName);
    }

    private void Update()
    {
        if (m_controller == null) return;
        ActiveAbility ability = m_controller.GetAbility(m_slotIndex);
        if (ability == null) return;
        if (m_cooldownFill == null) return;

        // Inverted: 0 right after use (bright icon gone), grows back to 1 as cooldown drains.
        float cd = ability.Cooldown;
        m_cooldownFill.fillAmount = cd > 0f
            ? 1f - Mathf.Clamp01(m_controller.GetCooldownRemaining(m_slotIndex) / cd)
            : 1f;
    }

    private void OnAbilityEquipped(int slotIndex, ActiveAbility ability)
    {
        if (slotIndex != m_slotIndex) return;

        SetRootActive(ability != null);

        if (ability == null) return;

        if (m_iconImage != null)
        {
            m_iconImage.sprite = ability.Icon;
            m_iconImage.color = m_dimmedIconColor;
            m_iconImage.enabled = ability.Icon != null;
        }

        if (m_cooldownFill != null)
        {
            m_cooldownFill.sprite = ability.Icon;
            m_cooldownFill.color = Color.white;
            m_cooldownFill.fillAmount = 1f;
        }
    }

    private void SetRootActive(bool active)
    {
        GameObject root = m_root != null ? m_root : gameObject;
        if (root.activeSelf != active)
            root.SetActive(active);
    }
}
