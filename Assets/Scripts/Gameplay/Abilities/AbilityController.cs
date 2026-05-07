using System;
using Gameplay.ObjectPool;
using Input;
using UnityEngine;
using UnityServiceLocator;

public class AbilityController : MonoBehaviour
{
    public const int SlotCount = 4;

    [SerializeField] private InputReader m_inputReader;
    [SerializeField] private EntityHealth m_health;
    [SerializeField] private PlayerStatusEffects m_status;

    private readonly ActiveAbility[] m_slots = new ActiveAbility[SlotCount];
    private readonly float[] m_cooldownRemaining = new float[SlotCount];
    private bool m_servicesCached;
    private ObjectPool m_pool;
    private CameraShake m_shake;
    private HitStopService m_hitStop;

    public event Action<int, ActiveAbility> OnAbilityEquipped;
    public event Action<int> OnAbilityUsed;
    public event Action<int> OnCooldownReady;

    public ActiveAbility GetAbility(int slotIndex)
        => InRange(slotIndex) ? m_slots[slotIndex] : null;

    public float GetCooldownRemaining(int slotIndex)
        => InRange(slotIndex) ? m_cooldownRemaining[slotIndex] : 0f;

    public bool IsReady(int slotIndex)
        => InRange(slotIndex) && m_slots[slotIndex] != null && m_cooldownRemaining[slotIndex] <= 0f;

    public bool HasAnyAbility()
    {
        for (int i = 0; i < SlotCount; i++)
            if (m_slots[i] != null) return true;
        return false;
    }

    // First empty slot, or -1 if all four are occupied. Pickup uses this to choose where to place a new ability.
    public int FindFirstEmptySlot()
    {
        for (int i = 0; i < SlotCount; i++)
            if (m_slots[i] == null) return i;
        return -1;
    }

    private void Awake()
    {
        ServiceLocator.Global.Register(this);
    }

    private void OnEnable()
    {
        if (m_inputReader != null)
            m_inputReader.UseAbility += TryUse;
    }

    private void OnDisable()
    {
        if (m_inputReader != null)
            m_inputReader.UseAbility -= TryUse;
    }

    private void Update()
    {
        Tick(Time.unscaledDeltaTime);
    }

    public void Tick(float dt)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (m_cooldownRemaining[i] <= 0f) continue;

            m_cooldownRemaining[i] -= dt;
            if (m_cooldownRemaining[i] > 0f) continue;

            m_cooldownRemaining[i] = 0f;
            OnCooldownReady?.Invoke(i);
        }
    }

    public void Equip(int slotIndex, ActiveAbility ability)
    {
        if (!InRange(slotIndex)) return;

        m_slots[slotIndex] = ability;
        m_cooldownRemaining[slotIndex] = 0f;
        OnAbilityEquipped?.Invoke(slotIndex, ability);
        if (ability != null)
            OnCooldownReady?.Invoke(slotIndex);
    }

    public void Reset()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            m_slots[i] = null;
            m_cooldownRemaining[i] = 0f;
            OnAbilityEquipped?.Invoke(i, null);
        }
    }

    public void TryUse(int slotIndex)
    {
        if (!IsReady(slotIndex)) return;

        ActiveAbility ability = m_slots[slotIndex];
        if (ability.Effect == null) return;

        if (!m_servicesCached)
        {
            ServiceLocator.Global.TryGet(out m_pool);
            ServiceLocator.Global.TryGet(out m_shake);
            ServiceLocator.Global.TryGet(out m_hitStop);
            m_servicesCached = true;
        }

        Vector2 aim = m_inputReader != null ? m_inputReader.LookDirection : Vector2.right;
        if (aim.sqrMagnitude < 0.0001f) aim = Vector2.right;

        AbilityContext ctx = new AbilityContext(
            transform,
            aim.normalized,
            m_health,
            m_status,
            m_pool,
            m_shake,
            m_hitStop,
            ability.TintColor);

        ability.Effect.Execute(in ctx);

        m_cooldownRemaining[slotIndex] = ability.Cooldown;
        OnAbilityUsed?.Invoke(slotIndex);
    }

    private static bool InRange(int slotIndex) => slotIndex >= 0 && slotIndex < SlotCount;
}
