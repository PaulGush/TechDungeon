using System;
using UnityEngine;
using UnityServiceLocator;

public class PlayerStatusEffects : MonoBehaviour
{
    public enum BuffKind
    {
        DamageMultiplier,
        SpeedMultiplier,
        Invulnerable
    }

    private const int KindCount = 3;

    private struct BuffSlot
    {
        public bool Active;
        public float Magnitude;       // 1.0-relative multiplier for *Multiplier kinds; ignored for Invulnerable
        public float TimeRemaining;
    }

    [SerializeField] private EntityHealth m_health;

    private readonly BuffSlot[] m_slots = new BuffSlot[KindCount];

    public event Action<BuffKind, float> OnBuffStarted;
    public event Action<BuffKind> OnBuffEnded;

    private void Awake()
    {
        ServiceLocator.Global.Register(this);
    }

    public bool IsActive(BuffKind kind) => m_slots[(int)kind].Active;

    // Returns 1f when no multiplier buff of the given kind is active, so callers can
    // unconditionally multiply through the modifier pipeline without null-checks.
    public float GetMultiplier(BuffKind kind)
    {
        BuffSlot slot = m_slots[(int)kind];
        return slot.Active ? slot.Magnitude : 1f;
    }

    public void ApplyTimed(BuffKind kind, float magnitude, float seconds)
    {
        if (seconds <= 0f) return;

        int index = (int)kind;
        bool wasActive = m_slots[index].Active;
        m_slots[index].Active = true;
        m_slots[index].Magnitude = magnitude;
        m_slots[index].TimeRemaining = seconds;

        if (wasActive) return;

        if (kind == BuffKind.Invulnerable && m_health != null)
            m_health.IsGodMode = true;

        OnBuffStarted?.Invoke(kind, seconds);
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    // Exposed for edit-mode tests to drive expiry without a play-mode Update loop.
    public void Tick(float dt)
    {
        for (int i = 0; i < m_slots.Length; i++)
        {
            if (!m_slots[i].Active) continue;

            m_slots[i].TimeRemaining -= dt;
            if (m_slots[i].TimeRemaining > 0f) continue;

            BuffKind kind = (BuffKind)i;
            m_slots[i].Active = false;
            m_slots[i].Magnitude = 0f;
            m_slots[i].TimeRemaining = 0f;

            if (kind == BuffKind.Invulnerable && m_health != null)
                m_health.IsGodMode = false;

            OnBuffEnded?.Invoke(kind);
        }
    }

    public void Clear()
    {
        for (int i = 0; i < m_slots.Length; i++)
        {
            if (!m_slots[i].Active) continue;
            BuffKind kind = (BuffKind)i;
            m_slots[i] = default;
            if (kind == BuffKind.Invulnerable && m_health != null)
                m_health.IsGodMode = false;
            OnBuffEnded?.Invoke(kind);
        }
    }
}
