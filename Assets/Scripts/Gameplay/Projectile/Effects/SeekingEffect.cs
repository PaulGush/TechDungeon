using System.Collections.Generic;
using UnityEngine;

public class SeekingEffect : IAmmoEffect
{
    private static readonly List<Collider2D> s_overlapResults = new List<Collider2D>();
    private static ContactFilter2D s_contactFilter;
    private static bool s_contactFilterInitialized;

    private readonly float m_seekRange;
    private readonly float m_turnRateDegPerSec;

    public SeekingEffect(float seekRange, float turnRateDegPerSec)
    {
        m_seekRange = seekRange;
        m_turnRateDegPerSec = turnRateDegPerSec;
    }

    public void OnHit(AmmoEffectContext ctx) { }

    public void OnDestroy(AmmoEffectContext ctx) { }

    public bool TryPreventDestroy(AmmoEffectContext ctx) => false;

    public void OnTick(AmmoEffectContext ctx)
    {
        if (ctx.Rigidbody == null) return;

        Vector2 velocity = ctx.Rigidbody.linearVelocity;
        float speed = velocity.magnitude;
        if (speed <= 0.0001f) return;

        Collider2D nearest = FindNearestTarget(ctx);
        if (nearest == null) return;

        Vector2 toTarget = ((Vector2)nearest.transform.position - ctx.Position).normalized;
        Vector2 currentDir = velocity / speed;

        // Cap rotation per fixed step so the projectile arcs toward the target instead
        // of snapping — gives the unmistakable "seeking missile" curve.
        float maxRadians = m_turnRateDegPerSec * Mathf.Deg2Rad * Time.fixedDeltaTime;
        Vector2 newDir = Vector3.RotateTowards(currentDir, toTarget, maxRadians, 0f);

        ctx.Rigidbody.linearVelocity = newDir * speed;
        // Keep the sprite aligned with motion so trail/sprite rotation match the new heading.
        float angle = Mathf.Atan2(newDir.y, newDir.x) * Mathf.Rad2Deg;
        ctx.Transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private Collider2D FindNearestTarget(AmmoEffectContext ctx)
    {
        if (!s_contactFilterInitialized)
        {
            s_contactFilter = new ContactFilter2D { useTriggers = true };
            s_contactFilterInitialized = true;
        }
        s_contactFilter.SetLayerMask(ctx.DamageLayers);

        int hitCount = Physics2D.OverlapCircle(ctx.Position, m_seekRange, s_contactFilter, s_overlapResults);
        Collider2D nearest = null;
        float nearestDistSqr = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = s_overlapResults[i];
            float distSqr = ((Vector2)hit.transform.position - ctx.Position).sqrMagnitude;
            if (distSqr >= nearestDistSqr) continue;

            nearestDistSqr = distSqr;
            nearest = hit;
        }

        return nearest;
    }
}
