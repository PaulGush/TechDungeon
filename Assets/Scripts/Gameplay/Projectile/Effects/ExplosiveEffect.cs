using UnityEngine;

public class ExplosiveEffect : IAmmoEffect
{
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

        if (m_explosionEffectPrefab != null)
        {
            GameObject effect = Object.Instantiate(m_explosionEffectPrefab, ctx.Position, Quaternion.identity);
            effect.transform.localScale = Vector3.one * (m_explosionRadius * 2f);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(ctx.Position, m_explosionRadius, ctx.DamageLayers);
        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent<EntityHealth>(out var health))
            {
                int damage = Mathf.RoundToInt((m_explosionDamage + ctx.BonusDamage) * ctx.DamageMultiplier);
                health.TakeDamage(damage);
            }
        }
    }

    public bool TryPreventDestroy(AmmoEffectContext ctx) => false;
}
