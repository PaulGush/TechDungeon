using System;
using UnityEngine;

[Serializable]
public class ProjectileBurstAbilityEffect : IAbilityEffect
{
    [SerializeField] private GameObject m_projectilePrefab;
    [SerializeField, Min(1)] private int m_projectileCount = 8;
    [SerializeField, Min(0)] private int m_bonusDamage = 4;
    [SerializeField, Min(0)] private int m_bonusPierce;

    public void Execute(in AbilityContext ctx)
    {
        if (ctx.Pool == null || m_projectilePrefab == null) return;

        // Even radial sweep with a random initial offset so consecutive bursts don't
        // produce identical projectile vectors.
        float step = 360f / m_projectileCount;
        float baseAngle = UnityEngine.Random.Range(0f, step);

        for (int i = 0; i < m_projectileCount; i++)
        {
            Quaternion rot = Quaternion.Euler(0f, 0f, baseAngle + i * step);
            ProjectileSpawner.Spawn(
                ctx.Pool,
                m_projectilePrefab,
                ctx.PlayerTransform.position,
                rot,
                bonusDamage: m_bonusDamage,
                bonusPierce: m_bonusPierce);
        }
    }
}
