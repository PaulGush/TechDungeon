using UnityEngine;

public class ChainLightningEffect : IAmmoEffect
{
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

        Vector2 direction = ((Vector2)nearest.transform.position - ctx.Position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        GameObject chainObj = ctx.Pool.GetPooledObject(ctx.ProjectilePrefab);
        chainObj.transform.SetPositionAndRotation(ctx.Position, Quaternion.Euler(0, 0, angle));

        Projectile chainProj = chainObj.GetComponent<Projectile>();
        chainProj.SetProjectilePrefab(ctx.ProjectilePrefab);
        chainProj.SetMutationModifiers(ctx.BonusDamage, ctx.DamageMultiplier, 0);
        chainProj.SetAmmoEffect(m_settings, new ChainLightningEffect(m_chainRange, m_chainsRemaining - 1, m_settings));
        chainProj.Initialize();
    }

    public void OnDestroy(AmmoEffectContext ctx) { }

    public bool TryPreventDestroy(AmmoEffectContext ctx) => false;

    private Collider2D FindNearestTarget(AmmoEffectContext ctx)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(ctx.Position, m_chainRange, ctx.DamageLayers);
        Collider2D nearest = null;
        float nearestDist = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            float dist = ((Vector2)hit.transform.position - ctx.Position).sqrMagnitude;
            if (dist > 0.01f && dist < nearestDist)
            {
                nearestDist = dist;
                nearest = hit;
            }
        }

        return nearest;
    }
}
