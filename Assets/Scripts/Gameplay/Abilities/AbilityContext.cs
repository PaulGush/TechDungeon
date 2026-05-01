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

    public AbilityContext(
        Transform playerTransform,
        Vector2 aimDirection,
        EntityHealth playerHealth,
        PlayerStatusEffects status,
        ObjectPool pool,
        CameraShake shake,
        HitStopService hitStop,
        Color tintColor)
    {
        PlayerTransform = playerTransform;
        AimDirection = aimDirection;
        PlayerHealth = playerHealth;
        Status = status;
        Pool = pool;
        Shake = shake;
        HitStop = hitStop;
        TintColor = tintColor;
    }
}
