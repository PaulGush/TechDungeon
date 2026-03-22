using UnityEngine;

public class RicochetEffect : IAmmoEffect
{
    private int m_bouncesRemaining;

    public RicochetEffect(int maxBounces)
    {
        m_bouncesRemaining = maxBounces;
    }

    public void OnHit(AmmoEffectContext ctx) { }

    public void OnDestroy(AmmoEffectContext ctx) { }

    public bool TryPreventDestroy(AmmoEffectContext ctx)
    {
        if (m_bouncesRemaining <= 0) return false;

        Vector2 velocity = ctx.Velocity;
        Vector2 direction = velocity.normalized;
        float speed = velocity.magnitude;

        // Step back along velocity to a point guaranteed outside the wall, then cast forward
        Vector2 rayOrigin = ctx.Position - direction * 2f;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, direction, 4f, ctx.DestroyLayers);
        if (hit.collider == null) return false;

        m_bouncesRemaining--;

        Vector2 reflected = Vector2.Reflect(direction, hit.normal);

        // Reposition just outside the wall along the surface normal
        ctx.Transform.position = (Vector3)(hit.point + hit.normal * 0.15f);
        ctx.Rigidbody.linearVelocity = reflected * speed;

        float angle = Mathf.Atan2(reflected.y, reflected.x) * Mathf.Rad2Deg;
        ctx.Transform.rotation = Quaternion.Euler(0, 0, angle);

        return true;
    }
}
