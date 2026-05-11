using Gameplay.ObjectPool;
using UnityEngine;

public readonly struct AbilityContext
{
    public readonly Transform PlayerTransform;
    public readonly Vector2 AimDirection;
    public readonly EntityHealth PlayerHealth;
    public readonly PlayerStatusEffects Status;
    public readonly ObjectPool Pool;
    public readonly CameraShake Shake;
    public readonly HitStopService HitStop;
    public readonly Color TintColor;

    // World position abilities should treat as the visual origin — typically the equipped
    // weapon's shoot point, falling back to the player's own position when nothing is equipped.
    public readonly Vector3 CastOrigin;

    public AbilityContext(
        Transform playerTransform,
        Vector2 aimDirection,
        EntityHealth playerHealth,
        PlayerStatusEffects status,
        ObjectPool pool,
        CameraShake shake,
        HitStopService hitStop,
        Color tintColor,
        Vector3 castOrigin)
    {
        PlayerTransform = playerTransform;
        AimDirection = aimDirection;
        PlayerHealth = playerHealth;
        Status = status;
        Pool = pool;
        Shake = shake;
        HitStop = hitStop;
        TintColor = tintColor;
        CastOrigin = castOrigin;
    }
}
