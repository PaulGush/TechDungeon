using System;
using Gameplay.ObjectPool;
using Input;
using PlayerObject;
using UnityEngine;
using UnityServiceLocator;

[RequireComponent(typeof(Lootable))]
public class Pickup : MonoBehaviour
{
    [SerializeField] private InputReader m_inputReader;

    private IPickupEffect m_effect;
    private IPickupTooltip m_tooltipSource;
    private Lootable m_lootable;
    private ObjectPool m_pool;
    private PlayerInteractionDisplay m_interactionDisplay;
    private Tooltip m_tooltip;

    private GameObject m_collector;
    private bool m_prompted;
    private bool m_tooltipShown;
    // Latches on the first successful Apply so a stray re-entry can't apply the effect twice
    // (that was filling two ability slots from a single ability drop).
    private bool m_collected;

    public Action OnPickedUp;

    private void Awake()
    {
        m_effect = GetComponent<IPickupEffect>();
        m_tooltipSource = GetComponent<IPickupTooltip>();
        m_lootable = GetComponent<Lootable>();
    }

    private void OnEnable()
    {
        ServiceLocator.Global.TryGet(out m_pool);
        m_collected = false;
        m_prompted = false;
        m_tooltipShown = false;
        m_collector = null;
    }

    private void OnDisable() => ClearPrompt();

    private void OnDestroy() => ClearPrompt();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (m_collected || m_prompted || m_effect == null) return;
        if (other.gameObject.layer != GameConstants.Layers.PlayerLayer) return;

        // Skip the prompt if the effect can't take right now (e.g. health pickup at full HP).
        // Player can walk off and back on once the condition changes — the next enter checks again.
        if (!m_effect.CanApply(other.gameObject)) return;

        m_collector = other.gameObject;
        m_prompted = true;

        if (m_interactionDisplay == null) ServiceLocator.Global.TryGet(out m_interactionDisplay);
        m_interactionDisplay?.Show("[Interact]", this);

        // Pickups that can describe themselves (items, abilities, health, ammo) always pop a tooltip.
        // The tooltip prefab decides its own on-screen position via its RectTransform anchors —
        // typically centred — so we don't pass an in-world anchor.
        if (m_tooltipSource != null && m_tooltipSource.TryGetTooltip(out string title, out string body, out string effect))
        {
            if (m_tooltip == null) ServiceLocator.Global.TryGet(out m_tooltip);
            if (m_tooltip != null)
            {
                m_tooltip.Show(title, body, effect, this);
                m_tooltipShown = true;
            }
        }

        if (m_inputReader != null) m_inputReader.Interact += Collect;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!m_prompted || other.gameObject.layer != GameConstants.Layers.PlayerLayer) return;
        ClearPrompt();
        m_prompted = false;
        m_collector = null;
    }

    private void Collect()
    {
        if (m_collected || m_collector == null || m_effect == null) return;
        // Only the top-of-stack prompt should respond — otherwise overlapping pickups would
        // all collect on a single Interact press.
        if (m_interactionDisplay != null && m_interactionDisplay.CurrentSource != this) return;
        // Still lerping into place after a chest/floor drop — wait for it to settle.
        if (m_lootable.IsSpawning) return;

        if (!m_effect.Apply(m_collector)) return;

        m_collected = true;
        ClearPrompt();

        if (m_lootable.OnCollected != null)
        {
            m_lootable.OnCollected.Invoke();
            m_lootable.OnCollected = null;
        }

        OnPickedUp?.Invoke();

        if (m_pool == null || !m_pool.ReturnGameObject(gameObject))
            Destroy(gameObject);
    }

    private void ClearPrompt()
    {
        m_interactionDisplay?.Hide(this);
        if (m_tooltipShown)
        {
            m_tooltip?.Hide(this);
            m_tooltipShown = false;
        }
        if (m_inputReader != null) m_inputReader.Interact -= Collect;
    }
}
