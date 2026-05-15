using System;
using UnityEngine;

[Serializable]
public class TempBuffAbilityEffect : IAbilityEffect
{
    [SerializeField] private PlayerStatusEffects.BuffKind m_kind = PlayerStatusEffects.BuffKind.Invulnerable;

    [Tooltip("Multiplier magnitude for *Multiplier kinds (e.g. 1.5 = +50% damage). Ignored for Invulnerable.")]
    [SerializeField] private float m_magnitude = 1f;

    [SerializeField, Min(0.05f)] private float m_durationSeconds = 3f;

    [SerializeField] private GameObject m_vfxPrefab;

    [Header("Shield Shimmer (Invulnerable kind only)")]
    [Tooltip("Intensity of the on-sprite shimmer while shielded. 0 disables, 1 is full strength.")]
    [SerializeField, Range(0f, 1f)] private float m_shimmerAmount = 1f;

    [Tooltip("If alpha > 0, overrides the shader's _ShimmerColor for this ability. Leave alpha 0 to keep whatever colour the material asset is set to.")]
    [SerializeField] private Color m_shimmerColor = new Color(0.3f, 0.7f, 1.4f, 0f);

    [Tooltip("Normalised remaining time (1=just cast, 0=about to expire) at which the pulse begins to speed up. 0.5 = the second half of the buff ramps.")]
    [SerializeField, Range(0.05f, 1f)] private float m_pulseAccelStartT = 0.5f;

    [Tooltip("Pulse speed multiplier at the very end of the buff. 1 disables the ramp; higher values give a frantic last-second pulse.")]
    [SerializeField, Min(1f)] private float m_pulseMaxMultiplier = 4f;

    public void Execute(in AbilityContext ctx)
    {
        if (ctx.Status == null) return;

        // Push the shimmer tuning to the player's ShieldShimmer before the buff starts so its
        // OnBuffStarted handler reads the ability-specific values. No-op if this isn't a shield
        // cast or the player has no ShieldShimmer attached.
        if (m_kind == PlayerStatusEffects.BuffKind.Invulnerable && ctx.PlayerTransform != null)
        {
            ShieldShimmer shimmer = ctx.PlayerTransform.GetComponentInChildren<ShieldShimmer>(true);
            if (shimmer != null)
                shimmer.Configure(m_shimmerAmount, m_shimmerColor, m_pulseAccelStartT, m_pulseMaxMultiplier);
        }

        ctx.Status.ApplyTimed(m_kind, m_magnitude, m_durationSeconds);

        if (m_vfxPrefab != null && ctx.Pool != null)
        {
            GameObject vfx = ctx.Pool.GetPooledObject(m_vfxPrefab, ctx.PlayerTransform.position, Quaternion.identity);
            if (vfx != null && vfx.TryGetComponent(out PooledEffect pe))
                pe.SetTint(ctx.TintColor);
        }
    }
}
