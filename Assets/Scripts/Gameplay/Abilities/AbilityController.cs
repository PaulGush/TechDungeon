using System;
using Gameplay.ObjectPool;
using Input;
using UnityEngine;
using UnityServiceLocator;

public class AbilityController : MonoBehaviour
{
    [SerializeField] private InputReader m_inputReader;
    [SerializeField] private EntityHealth m_health;
    [SerializeField] private PlayerStatusEffects m_status;

    private ActiveAbility m_current;
    private float m_cooldownRemaining;
    private bool m_servicesCached;
    private ObjectPool m_pool;
    private CameraShake m_shake;
    private HitStopService m_hitStop;

    public ActiveAbility Current => m_current;
    public float CooldownRemaining => m_cooldownRemaining;
    public bool IsReady => m_current != null && m_cooldownRemaining <= 0f;

    public event Action<ActiveAbility> OnAbilityEquipped;
    public event Action OnAbilityUsed;
    public event Action OnCooldownReady;

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

    // Exposed for edit-mode tests so cooldown progression can be exercised without a play loop.
    public void Tick(float dt)
    {
        if (m_cooldownRemaining <= 0f) return;

        m_cooldownRemaining -= dt;
        if (m_cooldownRemaining > 0f) return;

        m_cooldownRemaining = 0f;
        OnCooldownReady?.Invoke();
    }

    public void Equip(ActiveAbility ability)
    {
        m_current = ability;
        m_cooldownRemaining = 0f;
        OnAbilityEquipped?.Invoke(ability);
        if (ability != null)
            OnCooldownReady?.Invoke();
    }

    public void Reset()
    {
        m_current = null;
        m_cooldownRemaining = 0f;
        OnAbilityEquipped?.Invoke(null);
    }

    public void TryUse()
    {
        if (!IsReady) return;
        if (m_current.Effect == null) return;

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
            m_current.TintColor);

        m_current.Effect.Execute(in ctx);

        m_cooldownRemaining = m_current.Cooldown;
        OnAbilityUsed?.Invoke();
    }
}
