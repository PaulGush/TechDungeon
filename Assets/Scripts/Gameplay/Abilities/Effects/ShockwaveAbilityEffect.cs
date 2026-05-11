using System;
using UnityEngine;

[Serializable]
public class ShockwaveAbilityEffect : IAbilityEffect
{
    [SerializeField, Min(0.1f)] private float m_radius = 4f;
    [SerializeField, Min(0)] private int m_damage = 25;

    [SerializeField] private GameObject m_vfxPrefab;

    [SerializeField, Min(0f)] private float m_shakeAmplitude = 0.45f;
    [SerializeField, Min(0f)] private float m_hitStopSeconds = 0.08f;
    [SerializeField, Range(0f, 0.5f)] private float m_hitStopScale = 0.05f;

    public void Execute(in AbilityContext ctx)
    {
        Vector3 origin = ctx.PlayerTransform.position;

        // Pooled, self-returning VFX. PooledEffect handles the auto-return + tint reset for
        // sprite-based effects; ShockwaveVfx is the procedural expanding ring and needs the
        // blast radius so it draws exactly over the damage area.
        if (m_vfxPrefab != null && ctx.Pool != null)
        {
            GameObject vfx = ctx.Pool.GetPooledObject(m_vfxPrefab, origin, Quaternion.identity);
            if (vfx != null)
            {
                if (vfx.TryGetComponent(out PooledEffect pe))
                    pe.SetTint(ctx.TintColor);
                if (vfx.TryGetComponent(out ShockwaveVfx wave))
                    wave.Play(m_radius, ctx.TintColor);
            }
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, m_radius, GameConstants.Layers.EnemyLayerMask);
        bool didHit = false;
        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].TryGetComponent(out EntityHealth eh)) continue;
            eh.TakeDamage(m_damage);
            didHit = true;
        }

        // Punch the screen only on connection — a whiffed shockwave shouldn't freeze the camera.
        if (!didHit) return;

        if (ctx.HitStop != null && m_hitStopSeconds > 0f)
            ctx.HitStop.Freeze(m_hitStopSeconds, m_hitStopScale);
        if (ctx.Shake != null && m_shakeAmplitude > 0f)
            ctx.Shake.Shake(m_shakeAmplitude);
    }
}
