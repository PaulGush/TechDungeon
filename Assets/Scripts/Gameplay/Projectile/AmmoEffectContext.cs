using Gameplay.ObjectPool;
using UnityEngine;

public struct AmmoEffectContext
{
    public Vector2 Position;
    public Vector2 Velocity;
    public int BonusDamage;
    public float DamageMultiplier;
    public LayerMask DamageLayers;
    public LayerMask DestroyLayers;
    public ObjectPool Pool;
    public GameObject ProjectilePrefab;
    public Rigidbody2D Rigidbody;
    public Transform Transform;

    // The projectile's trail, if it has one. Effects that teleport the projectile (ricochet)
    // should Clear() this so the trail doesn't draw a streak across the jump.
    public TrailRenderer Trail;
}
