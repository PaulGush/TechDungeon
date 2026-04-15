using System.Collections.Generic;
using UnityEngine;

public class ExplosiveEffect : IAmmoEffect
{
    private const float VisualDiameterMultiplier = 2f;

    private static readonly List<Collider2D> s_overlapResults = new List<Collider2D>();
    private static ContactFilter2D s_contactFilter;
    private static bool s_contactFilterInitialized;

    private readonly float m_explosionRadius;
    private readonly int m_explosionDamage;
    private readonly GameObject m_explosionEffectPrefab;

    public ExplosiveEffect(float explosionRadius, int explosionDamage, GameObject explosionEffectPrefab)
    {
        m_explosionRadius = explosionRadius;
        m_explosionDamage = explosionDamage;
        m_explosionEffectPrefab = explosionEffectPrefab;
    }

    public void OnHit(AmmoEffectContext ctx) { }

    public void OnDestroy(AmmoEffectContext ctx)
    {
        if (m_explosionRadius <= 0f) return;

        SpawnVisual(ctx.Position);
        ApplyDamage(ctx);
    }

    public bool TryPreventDestroy(AmmoEffectContext ctx) => false;

    private void SpawnVisual(Vector2 position)
    {
        if (m_explosionEffectPrefab == null) return;

        GameObject effect = Object.Instantiate(m_explosionEffectPrefab, position, Quaternion.identity);
        effect.transform.localScale = Vector3.one * (m_explosionRadius * VisualDiameterMultiplier);
    }

    private void ApplyDamage(AmmoEffectContext ctx)
    {
        if (!s_contactFilterInitialized)
        {
            s_contactFilter = new ContactFilter2D { useTriggers = true };
            s_contactFilterInitialized = true;
        }
        s_contactFilter.SetLayerMask(ctx.DamageLayers);

        int hitCount = Physics2D.OverlapCircle(ctx.Position, m_explosionRadius, s_contactFilter, s_overlapResults);
        for (int i = 0; i < hitCount; i++)
        {
            if (!s_overlapResults[i].TryGetComponent(out EntityHealth health)) continue;

            int damage = Mathf.RoundToInt((m_explosionDamage + ctx.BonusDamage) * ctx.DamageMultiplier);
            health.TakeDamage(damage);
        }
    }
}
