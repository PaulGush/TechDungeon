using UnityEngine;

public class RicochetEffect : IAmmoEffect
{
    private readonly float m_rayBackOffset;
    private readonly float m_rayDistance;
    private readonly float m_surfaceClearance;
    private int m_bouncesRemaining;

    public RicochetEffect(int maxBounces, float rayBackOffset, float rayDistance, float surfaceClearance)
    {
        m_bouncesRemaining = maxBounces;
        m_rayBackOffset = rayBackOffset;
        m_rayDistance = rayDistance;
        m_surfaceClearance = surfaceClearance;
    }

    public void OnHit(AmmoEffectContext ctx) { }

    public void OnTick(AmmoEffectContext ctx) { }

    public void OnDestroy(AmmoEffectContext ctx) { }

    public bool TryPreventDestroy(AmmoEffectContext ctx)
    {
        if (m_bouncesRemaining <= 0) return false;

        Vector2 velocity = ctx.Velocity;
        Vector2 direction = velocity.normalized;
        float speed = velocity.magnitude;

        // Only ever ricochet off level geometry — never off an enemy. A missile's destroy mask
        // includes the Enemy layer (so it detonates on contact), but it must not bounce off the
        // boss; so cast against the Walls layer specifically rather than ctx.DestroyLayers.
        // Start the cast behind the projectile so the ray begins outside any wall it embedded in.
        Vector2 rayOrigin = ctx.Position - direction * m_rayBackOffset;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, direction, m_rayDistance, GameConstants.Layers.WallsLayerMask);
        if (hit.collider == null) return false;

        m_bouncesRemaining--;

        Vector2 reflected = Vector2.Reflect(direction, hit.normal);
        Vector2 landing = hit.point + hit.normal * m_surfaceClearance;
        float angle = Mathf.Atan2(reflected.y, reflected.x) * Mathf.Rad2Deg;

        // Teleport via the Rigidbody2D (the authoritative position for a physics-driven
        // projectile) — and flush interpolation off→on around it — so the next render frame
        // doesn't interpolate from the pre-bounce position. Also set the Transform directly so
        // it's already correct for this frame's trail sample. Then Clear() the trail so it
        // restarts cleanly at the bounce point instead of streaking across the jump.
        if (ctx.Rigidbody != null)
        {
            ctx.Rigidbody.interpolation = RigidbodyInterpolation2D.None;
            ctx.Rigidbody.position = landing;
            ctx.Rigidbody.rotation = angle;
            ctx.Rigidbody.linearVelocity = reflected * speed;
            ctx.Rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
        ctx.Transform.position = (Vector3)landing;
        ctx.Transform.rotation = Quaternion.Euler(0f, 0f, angle);
        ctx.Trail?.Clear();

        return true;
    }
}
