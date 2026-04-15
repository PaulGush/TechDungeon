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

    public void OnDestroy(AmmoEffectContext ctx) { }

    public bool TryPreventDestroy(AmmoEffectContext ctx)
    {
        if (m_bouncesRemaining <= 0) return false;

        Vector2 velocity = ctx.Velocity;
        Vector2 direction = velocity.normalized;
        float speed = velocity.magnitude;

        // Start the cast behind the projectile so the ray begins outside any wall it just embedded in.
        Vector2 rayOrigin = ctx.Position - direction * m_rayBackOffset;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, direction, m_rayDistance, ctx.DestroyLayers);
        if (hit.collider == null) return false;

        m_bouncesRemaining--;

        Vector2 reflected = Vector2.Reflect(direction, hit.normal);

        ctx.Transform.position = (Vector3)(hit.point + hit.normal * m_surfaceClearance);
        ctx.Rigidbody.linearVelocity = reflected * speed;

        float angle = Mathf.Atan2(reflected.y, reflected.x) * Mathf.Rad2Deg;
        ctx.Transform.rotation = Quaternion.Euler(0f, 0f, angle);

        return true;
    }
}
