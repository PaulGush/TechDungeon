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
        if (m_explosionEffectPrefab == null) return;

        GameObject effectObj = ctx.Pool != null
            ? ctx.Pool.GetPooledObject(m_explosionEffectPrefab)
            : Object.Instantiate(m_explosionEffectPrefab);
        effectObj.transform.position = ctx.Position;
        effectObj.transform.rotation = Quaternion.identity;

        if (!effectObj.TryGetComponent(out ExplosionEffect explosion)) return;

        int damage = Mathf.RoundToInt((m_explosionDamage + ctx.BonusDamage) * ctx.DamageMultiplier);
        explosion.Initialize(m_explosionRadius, damage, ctx.DamageLayers);
    }

    public bool TryPreventDestroy(AmmoEffectContext ctx) => false;
}
