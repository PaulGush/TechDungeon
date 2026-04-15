using System;
using UnityEngine;

public enum BossAttackType
{
    Projectile,
    Flamethrower,
    Burst,
    MissileBarrage
}

[Serializable]
public class BossPhase
{
    [Range(0f, 1f)]
    [Tooltip("Phase activates when health drops to this percentage (e.g., 0.6 = 60%)")]
    public float HealthThreshold = 1f;

    public BossAttackType AttackType = BossAttackType.Projectile;

    [Tooltip("Index into the boss torso animator's Attack blend tree (the AttackIndex parameter). Lets each phase choose its attack animation independently of phase order.")]
    public int AttackAnimationIndex = 0;

    [Tooltip("-1 = use base settings")]
    public float FireRateOverride = -1f;

    [Tooltip("How long the flamethrower stays active per attack")]
    public float FlameDuration = 1.5f;
    [Tooltip("Damage per tick while in the flame cone")]
    public int FlameDamagePerTick = 1;
    [Tooltip("Time between damage ticks")]
    public float FlameTickInterval = 0.2f;

    [Tooltip("Number of projectiles in the burst ring")]
    public int BurstProjectileCount = 8;
    [Tooltip("Damage radius of the burst")]
    public float BurstRadius = 3f;

    [Header("Missile Barrage")]
    [Tooltip("Number of missiles fired per volley.")]
    public int MissileCount = 5;
    [Tooltip("Maximum distance from the player at which a missile can be aimed to land.")]
    public float MissileSpreadRadius = 4f;
    [Tooltip("Minimum distance between two missile landing sites — prevents indicators from overlapping.")]
    public float MissileMinSeparation = 1.5f;
    [Tooltip("How long the landing-site indicators stay on screen before missiles begin firing.")]
    public float MissileTelegraphDuration = 1.25f;
    [Tooltip("Time each missile takes to travel from the launch point to its target.")]
    public float MissileTravelDuration = 0.85f;
    [Tooltip("Apex height of the missile arc as a fraction of horizontal travel distance.")]
    public float MissileArcHeightRatio = 0.4f;
    [Tooltip("Damage dealt to entities inside the explosion radius on impact.")]
    public int MissileDamage = 4;
    [Tooltip("Radius of the explosion that damages entities on impact.")]
    public float MissileExplosionRadius = 1.25f;
    [Tooltip("Optional explosion VFX prefab spawned at each impact point.")]
    public GameObject MissileExplosionEffectPrefab;

    [Tooltip("-1 = use base settings")]
    public float SpeedOverride = -1f;
    [Tooltip("-1 = use base settings")]
    public float StrafeSpeedOverride = -1f;
    [Tooltip("-1 = use base settings")]
    public float AttackRangeOverride = -1f;
    [Tooltip("-1 = use base settings")]
    public float PreferredAttackDistanceOverride = -1f;
    public bool AggressiveChase;

    public bool SummonsMinions;
    public int MinionCount;
    public GameObject MinionPrefab;
}
