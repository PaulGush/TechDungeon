using System.Collections;
using TMPro;
using UI.InputPrompts;
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

    [Header("Prompts")]
    [Tooltip("Key prompt label (e.g. \"1\" / \"2\"). Shown when active device is keyboard&mouse, hidden on controller. Leave empty to skip.")]
    [SerializeField] private TextMeshProUGUI m_keyPrompt;

    [Tooltip("D-pad direction icon shown when active device is a controller, hidden on keyboard&mouse. Assign the matching D-pad direction sprite per slot. Leave empty to skip.")]
    [SerializeField] private Image m_dpadPrompt;

    [Header("Animation")]
    [Tooltip("Optional Animator on the slot. Receives a 'Press' trigger each time an ability fires — drives the bulge animation. Leave empty to skip.")]
    [SerializeField] private Animator m_animator;

    [Tooltip("Trigger parameter on the animator fired when the ability is used.")]
    [SerializeField] private string m_pressTriggerName = "Press";

    [Tooltip("Tint applied to the icon underneath when the ability is on cooldown — the dimmed 'unavailable' state. Stays applied while the bright top sprite radial-fills back to ready.")]
    [SerializeField] private Color m_dimmedIconColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    [Header("Ready Sheen")]
    [Tooltip("Diagonal sheen overlay. Slid bottom-left → top-right when this slot's cooldown finishes draining. Leave empty to skip the sheen.")]
    [SerializeField] private RectTransform m_sheenRect;

    [Tooltip("CanvasGroup on the sheen, used for fade in/out across the sweep. Without one the sheen stays at its authored alpha through the whole sweep.")]
    [SerializeField] private CanvasGroup m_sheenGroup;

    [Tooltip("Sheen anchoredPosition at sweep start. Default sits below-left of a 60x60 slot so the streak enters from the bottom-left corner.")]
    [SerializeField] private Vector2 m_sheenStartOffset = new Vector2(-60f, -60f);

    [Tooltip("Sheen anchoredPosition at sweep end. Default exits above-right of a 60x60 slot.")]
    [SerializeField] private Vector2 m_sheenEndOffset = new Vector2(60f, 60f);

    [Min(0.05f)]
    [Tooltip("Sweep duration in seconds. Runs on unscaled time so it plays through any active hit-stop.")]
    [SerializeField] private float m_sheenDuration = 0.4f;

    private AbilityController m_controller;
    private Coroutine m_sheenCoroutine;
    // Set when this slot fires the ability and cleared when the resulting cooldown drains, so we
    // only sheen the post-cooldown moment (not the implicit "ready" emitted on Equip).
    private bool m_pendingSheen;

    private void Awake()
    {
        HideSheen();
    }

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
        m_controller.OnCooldownReady += OnCooldownReady;
        OnAbilityEquipped(m_slotIndex, m_controller.GetAbility(m_slotIndex));
    }

    private void OnEnable()
    {
        ActiveDeviceTracker.DeviceChanged += OnDeviceChanged;
        ApplyDevicePrompts(ActiveDeviceTracker.Current);
    }

    private void OnDisable()
    {
        ActiveDeviceTracker.DeviceChanged -= OnDeviceChanged;
    }

    private void OnDestroy()
    {
        if (m_controller != null)
        {
            m_controller.OnAbilityEquipped -= OnAbilityEquipped;
            m_controller.OnAbilityUsed -= OnAbilityUsed;
            m_controller.OnCooldownReady -= OnCooldownReady;
        }
    }

    private void OnDeviceChanged(ActiveDevice device) => ApplyDevicePrompts(device);

    private void ApplyDevicePrompts(ActiveDevice device)
    {
        bool keyboard = device == ActiveDevice.KeyboardMouse;
        if (m_keyPrompt != null) m_keyPrompt.gameObject.SetActive(keyboard);
        if (m_dpadPrompt != null) m_dpadPrompt.gameObject.SetActive(!keyboard);
    }

    private void OnAbilityUsed(int slotIndex)
    {
        if (slotIndex != m_slotIndex) return;
        m_pendingSheen = true;
        if (m_animator != null && !string.IsNullOrEmpty(m_pressTriggerName))
            m_animator.SetTrigger(m_pressTriggerName);
    }

    private void OnCooldownReady(int slotIndex)
    {
        if (slotIndex != m_slotIndex) return;
        if (!m_pendingSheen) return;
        m_pendingSheen = false;

        if (m_sheenRect == null) return;
        if (m_sheenCoroutine != null) StopCoroutine(m_sheenCoroutine);
        m_sheenCoroutine = StartCoroutine(SheenSweep());
    }

    private IEnumerator SheenSweep()
    {
        m_sheenRect.anchoredPosition = m_sheenStartOffset;
        if (m_sheenGroup != null) m_sheenGroup.alpha = 0f;

        float t = 0f;
        while (t < m_sheenDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / m_sheenDuration);

            m_sheenRect.anchoredPosition = Vector2.LerpUnclamped(m_sheenStartOffset, m_sheenEndOffset, p);
            if (m_sheenGroup != null)
                m_sheenGroup.alpha = p < 0.5f ? p * 2f : (1f - p) * 2f;

            yield return null;
        }

        HideSheen();
        m_sheenCoroutine = null;
    }

    private void HideSheen()
    {
        if (m_sheenGroup != null) m_sheenGroup.alpha = 0f;
        if (m_sheenRect != null) m_sheenRect.anchoredPosition = m_sheenStartOffset;
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
