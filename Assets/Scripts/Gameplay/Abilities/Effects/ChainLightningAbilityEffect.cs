using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ChainLightningAbilityEffect : IAbilityEffect
{
    [SerializeField, Min(0.1f)] private float m_initialRange = 6f;

    [Tooltip("Search radius around each chained enemy when picking the next link.")]
    [SerializeField, Min(0.1f)] private float m_chainRange = 4f;

    [SerializeField, Min(1)] private int m_maxChains = 4;

    [SerializeField, Min(0)] private int m_damagePerHit = 20;

    [Tooltip("Optional pooled VFX spawned at each chained enemy (uses PooledEffect.SetTint).")]
    [SerializeField] private GameObject m_hitVfxPrefab;

    [Tooltip("Optional pooled arc prefab spawned between each link (player → first hit, then hit → hit). Must carry a ChainLightningArc component.")]
    [SerializeField] private GameObject m_arcVfxPrefab;

    [SerializeField, Min(0f)] private float m_shakeAmplitude = 0.3f;
    [SerializeField, Min(0f)] private float m_hitStopSeconds = 0.06f;
    [SerializeField, Range(0f, 0.5f)] private float m_hitStopScale = 0.05f;

    private static readonly List<Collider2D> s_overlapResults = new List<Collider2D>();
    private static ContactFilter2D s_contactFilter;
    private static bool s_contactFilterInitialized;
    private static readonly HashSet<EntityHealth> s_visited = new HashSet<EntityHealth>();

    public void Execute(in AbilityContext ctx)
    {
        s_visited.Clear();

        // Detection range is anchored to the player so a barrel that's pointing away from a
        // nearby enemy doesn't put them out of reach. The visual originates at the cast point
        // (weapon barrel when one's equipped) for readability.
        Vector2 origin = ctx.PlayerTransform.position;
        EntityHealth current = FindNearest(origin, m_initialRange, skipVisited: false);
        if (current == null) return;

        Vector3 prevPos = ctx.CastOrigin;
        bool firstHit = true;
        for (int i = 0; i < m_maxChains && current != null; i++)
        {
            s_visited.Add(current);
            current.TakeDamage(m_damagePerHit);

            Vector3 hitPos = current.transform.position;

            if (m_arcVfxPrefab != null && ctx.Pool != null)
            {
                GameObject arc = ctx.Pool.GetPooledObject(m_arcVfxPrefab, prevPos, Quaternion.identity);
                if (arc != null && arc.TryGetComponent(out ChainLightningArc arcVis))
                {
                    arcVis.SetTint(ctx.TintColor);
                    arcVis.Play(prevPos, hitPos);
                }
            }

            if (m_hitVfxPrefab != null && ctx.Pool != null)
            {
                GameObject vfx = ctx.Pool.GetPooledObject(m_hitVfxPrefab, hitPos, Quaternion.identity);
                if (vfx != null && vfx.TryGetComponent(out PooledEffect pe))
                    pe.SetTint(ctx.TintColor);
            }

            if (firstHit)
            {
                if (ctx.HitStop != null && m_hitStopSeconds > 0f)
                    ctx.HitStop.Freeze(m_hitStopSeconds, m_hitStopScale);
                if (ctx.Shake != null && m_shakeAmplitude > 0f)
                    ctx.Shake.Shake(m_shakeAmplitude);
                firstHit = false;
            }

            prevPos = hitPos;
            current = FindNearest(hitPos, m_chainRange, skipVisited: true);
        }
    }

    private static EntityHealth FindNearest(Vector2 origin, float range, bool skipVisited)
    {
        if (!s_contactFilterInitialized)
        {
            s_contactFilter = new ContactFilter2D { useTriggers = true };
            s_contactFilterInitialized = true;
        }
        s_contactFilter.SetLayerMask(GameConstants.Layers.EnemyLayerMask);

        int hitCount = Physics2D.OverlapCircle(origin, range, s_contactFilter, s_overlapResults);
        EntityHealth nearest = null;
        float nearestDistSqr = float.MaxValue;
        for (int i = 0; i < hitCount; i++)
        {
            if (!s_overlapResults[i].TryGetComponent(out EntityHealth eh)) continue;
            if (skipVisited && s_visited.Contains(eh)) continue;
            float distSqr = ((Vector2)eh.transform.position - origin).sqrMagnitude;
            if (distSqr >= nearestDistSqr) continue;
            nearestDistSqr = distSqr;
            nearest = eh;
        }
        return nearest;
    }
}
