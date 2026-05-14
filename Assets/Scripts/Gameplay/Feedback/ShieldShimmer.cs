using UnityEngine;
using UnityServiceLocator;

/// <summary>
/// Drives the Phase Shield visual on the player's sprite: instead of a ring around the player,
/// the player's <see cref="SpriteRenderer"/> material gets a blue scan-shimmer applied while
/// <see cref="PlayerStatusEffects.BuffKind.Invulnerable"/> is active. Works by toggling the
/// <c>_ShimmerAmount</c> property on the renderer's material instance — the shader handles the
/// scanline + pulse animation, so the only per-frame work here is ramping up the pulse speed as
/// the buff runs out so the player has a clear "shield is about to drop" telegraph.
/// <para>
/// Tuning (intensity, colour, pulse ramp) is supplied by the ability that triggers the buff —
/// see <see cref="TempBuffAbilityEffect"/>, which calls <see cref="Configure"/> before applying
/// the buff. The defaults below are a safety net for buffs that fire without going through an
/// ability (tests, debug toggles, etc.).
/// </para>
/// </summary>
public class ShieldShimmer : MonoBehaviour
{
    private static readonly int ShimmerAmountId = Shader.PropertyToID("_ShimmerAmount");
    private static readonly int ShimmerColorId = Shader.PropertyToID("_ShimmerColor");
    private static readonly int ShimmerPulseSpeedId = Shader.PropertyToID("_ShimmerPulseSpeed");

    [Header("References")]
    [Tooltip("SpriteRenderer whose material will have _ShimmerAmount driven. Material must use the SpriteHitFlash shader.")]
    [SerializeField] private SpriteRenderer m_renderer;

    // Runtime-configured by the ability that triggers the buff (TempBuffAbilityEffect.Configure).
    // Defaults here are only used if a buff is applied outside the normal ability pipeline.
    private float m_amount = 1f;
    private Color m_shimmerColor = new Color(0.3f, 0.7f, 1.4f, 0f);  // alpha 0 = keep material default
    private float m_pulseAccelStartT = 0.5f;
    private float m_pulseMaxMultiplier = 4f;

    private PlayerStatusEffects m_status;
    private Material m_materialInstance;
    private float m_basePulseSpeed;
    private float m_buffTotalDuration;
    private bool m_active;

    private Material MaterialInstance
    {
        get
        {
            if (m_materialInstance == null && m_renderer != null)
                m_materialInstance = m_renderer.material;
            return m_materialInstance;
        }
    }

    private void Reset() => m_renderer = GetComponent<SpriteRenderer>();

    private void Awake()
    {
        if (m_renderer == null) m_renderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (m_renderer != null && m_renderer.sharedMaterial != null
            && m_renderer.sharedMaterial.HasProperty(ShimmerPulseSpeedId))
            m_basePulseSpeed = m_renderer.sharedMaterial.GetFloat(ShimmerPulseSpeedId);
        if (m_basePulseSpeed <= 0f) m_basePulseSpeed = 4f;

        if (!ServiceLocator.Global.TryGet(out m_status)) return;

        m_status.OnBuffStarted += OnBuffStarted;
        m_status.OnBuffEnded += OnBuffEnded;

        if (m_status.IsActive(PlayerStatusEffects.BuffKind.Invulnerable))
        {
            m_buffTotalDuration = Mathf.Max(0.0001f, m_status.GetTimeRemaining(PlayerStatusEffects.BuffKind.Invulnerable));
            BeginShimmer();
        }
        else
        {
            ApplyShimmer(false);
        }
    }

    private void OnDestroy()
    {
        if (m_status == null) return;
        m_status.OnBuffStarted -= OnBuffStarted;
        m_status.OnBuffEnded -= OnBuffEnded;

        if (m_materialInstance != null)
        {
            m_materialInstance.SetFloat(ShimmerAmountId, 0f);
            m_materialInstance.SetFloat(ShimmerPulseSpeedId, m_basePulseSpeed);
        }
    }

    /// <summary>
    /// Push the visual tuning supplied by the firing ability. The buff that follows will read
    /// these values when it starts. Pass <c>shimmerColor.a == 0</c> to keep whatever <c>_ShimmerColor</c>
    /// the material asset already has.
    /// </summary>
    public void Configure(float amount, Color shimmerColor, float pulseAccelStartT, float pulseMaxMultiplier)
    {
        m_amount = Mathf.Clamp01(amount);
        m_shimmerColor = shimmerColor;
        m_pulseAccelStartT = Mathf.Clamp(pulseAccelStartT, 0.05f, 1f);
        m_pulseMaxMultiplier = Mathf.Max(1f, pulseMaxMultiplier);
    }

    private void OnBuffStarted(PlayerStatusEffects.BuffKind kind, float seconds)
    {
        if (kind != PlayerStatusEffects.BuffKind.Invulnerable) return;
        m_buffTotalDuration = Mathf.Max(0.0001f, seconds);
        BeginShimmer();
    }

    private void OnBuffEnded(PlayerStatusEffects.BuffKind kind)
    {
        if (kind == PlayerStatusEffects.BuffKind.Invulnerable) ApplyShimmer(false);
    }

    private void BeginShimmer()
    {
        m_active = true;
        ApplyShimmer(true);
    }

    private void Update()
    {
        if (!m_active || m_status == null) return;

        Material mat = MaterialInstance;
        if (mat == null) return;

        float remaining = m_status.GetTimeRemaining(PlayerStatusEffects.BuffKind.Invulnerable);
        float normalisedRemaining = Mathf.Clamp01(remaining / m_buffTotalDuration);

        float rampT = m_pulseAccelStartT > 0f
            ? Mathf.Clamp01((m_pulseAccelStartT - normalisedRemaining) / m_pulseAccelStartT)
            : 0f;
        float multiplier = Mathf.Lerp(1f, m_pulseMaxMultiplier, rampT);
        mat.SetFloat(ShimmerPulseSpeedId, m_basePulseSpeed * multiplier);
    }

    private void ApplyShimmer(bool on)
    {
        Material mat = MaterialInstance;
        if (mat == null) return;

        mat.SetFloat(ShimmerAmountId, on ? m_amount : 0f);
        if (on && m_shimmerColor.a > 0f)
            mat.SetColor(ShimmerColorId, m_shimmerColor);

        if (!on)
        {
            mat.SetFloat(ShimmerPulseSpeedId, m_basePulseSpeed);
            m_active = false;
        }
    }
}
