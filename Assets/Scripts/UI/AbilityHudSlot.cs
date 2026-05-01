using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityServiceLocator;

public class AbilityHudSlot : MonoBehaviour
{
    [Header("References")]
    [Tooltip("GameObject toggled off when no ability is equipped. Leave empty to toggle this HUD's own GameObject.")]
    [SerializeField] private GameObject m_root;

    [SerializeField] private Image m_iconImage;

    [Tooltip("Image with FillMethod = Radial360. Sweeps back from 0 to 1 as cooldown drains so the ability lights up when ready.")]
    [SerializeField] private Image m_cooldownFill;

    [Tooltip("Optional key prompt label (e.g. 'Q'). Leave empty to skip.")]
    [SerializeField] private TextMeshProUGUI m_keyPrompt;

    [Tooltip("Optional Animator on the slot. Receives a 'Press' trigger each time an ability fires — drives the bulge animation. Leave empty to skip.")]
    [SerializeField] private Animator m_animator;

    [Tooltip("Trigger parameter on the animator fired when the ability is used.")]
    [SerializeField] private string m_pressTriggerName = "Press";

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
        OnAbilityEquipped(m_controller.Current);
    }

    private void OnDestroy()
    {
        if (m_controller != null)
        {
            m_controller.OnAbilityEquipped -= OnAbilityEquipped;
            m_controller.OnAbilityUsed -= OnAbilityUsed;
        }
    }

    private void OnAbilityUsed()
    {
        if (m_animator != null && !string.IsNullOrEmpty(m_pressTriggerName))
            m_animator.SetTrigger(m_pressTriggerName);
    }

    private void Update()
    {
        if (m_controller == null || m_controller.Current == null) return;
        if (m_cooldownFill == null) return;

        // Inverted: 0 right after use (bright icon gone), grows back to 1 as cooldown drains.
        float cd = m_controller.Current.Cooldown;
        m_cooldownFill.fillAmount = cd > 0f
            ? 1f - Mathf.Clamp01(m_controller.CooldownRemaining / cd)
            : 1f;
    }

    [Tooltip("Tint applied to the icon underneath when the ability is on cooldown — the dimmed 'unavailable' state. Stays applied while the bright top sprite radial-fills back to ready.")]
    [SerializeField] private Color m_dimmedIconColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    private void OnAbilityEquipped(ActiveAbility ability)
    {
        SetRootActive(ability != null);

        if (ability == null) return;

        // Bottom icon: dimmed version of the sprite, always visible.
        if (m_iconImage != null)
        {
            m_iconImage.sprite = ability.Icon;
            m_iconImage.color = m_dimmedIconColor;
            m_iconImage.enabled = ability.Icon != null;
        }

        // Top cooldown layer: same sprite at full brightness. fillAmount inverts in Update —
        // 0 right after use (top hidden, dim icon shows through), 1 when ready (covers dim).
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
