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

    public void Execute(in AbilityContext ctx)
    {
        if (ctx.Status == null) return;

        ctx.Status.ApplyTimed(m_kind, m_magnitude, m_durationSeconds);

        if (m_vfxPrefab != null && ctx.Pool != null)
        {
            GameObject vfx = ctx.Pool.GetPooledObject(m_vfxPrefab, ctx.PlayerTransform.position, Quaternion.identity);
            if (vfx != null && vfx.TryGetComponent(out PooledEffect pe))
                pe.SetTint(ctx.TintColor);
        }
    }
}
