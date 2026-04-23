using System.Collections.Generic;
using UnityEngine;

public class ChainLightningEffect : IAmmoEffect
{
    private const float MinChainDistanceSqr = 0.01f;

    private static readonly List<Collider2D> s_overlapResults = new List<Collider2D>();
    private static ContactFilter2D s_contactFilter;
    private static bool s_contactFilterInitialized;

    private readonly float m_chainRange;
    private readonly AmmoSettings m_settings;
    private int m_chainsRemaining;

    public ChainLightningEffect(float chainRange, int maxChains, AmmoSettings settings)
    {
        m_chainRange = chainRange;
        m_chainsRemaining = maxChains;
        m_settings = settings;
    }

    public void OnHit(AmmoEffectContext ctx)
    {
        if (m_chainsRemaining <= 0 || ctx.Pool == null || ctx.ProjectilePrefab == null) return;

        Collider2D nearest = FindNearestTarget(ctx);
        if (nearest == null) return;

        SpawnChainProjectile(ctx, nearest);
    }

    public void OnDestroy(AmmoEffectContext ctx) { }

    public bool TryPreventDestroy(AmmoEffectContext ctx) => false;

    private void SpawnChainProjectile(AmmoEffectContext ctx, Collider2D target)
    {
        Vector2 direction = ((Vector2)target.transform.position - ctx.Position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        ProjectileSpawner.Spawn(
            ctx.Pool, ctx.ProjectilePrefab, ctx.Position, Quaternion.Euler(0f, 0f, angle),
            bonusDamage: ctx.BonusDamage, damageMultiplier: ctx.DamageMultiplier,
            ammoSettings: m_settings,
            ammoEffect: new ChainLightningEffect(m_chainRange, m_chainsRemaining - 1, m_settings));
    }

    private Collider2D FindNearestTarget(AmmoEffectContext ctx)
    {
        if (!s_contactFilterInitialized)
        {
            s_contactFilter = new ContactFilter2D { useTriggers = true };
            s_contactFilterInitialized = true;
        }
        s_contactFilter.SetLayerMask(ctx.DamageLayers);

        int hitCount = Physics2D.OverlapCircle(ctx.Position, m_chainRange, s_contactFilter, s_overlapResults);
        Collider2D nearest = null;
        float nearestDistSqr = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = s_overlapResults[i];
            float distSqr = ((Vector2)hit.transform.position - ctx.Position).sqrMagnitude;
            if (distSqr <= MinChainDistanceSqr || distSqr >= nearestDistSqr) continue;

            nearestDistSqr = distSqr;
            nearest = hit;
        }

        return nearest;
    }
}
