using System;
using UnityEngine;

public enum BossAttackType
{
    Projectile,
    Flamethrower,
    Burst
}

[Serializable]
public class BossPhase
{
    [Range(0f, 1f)]
    [Tooltip("Phase activates when health drops to this percentage (e.g., 0.6 = 60%)")]
    public float HealthThreshold = 1f;

    public BossAttackType AttackType = BossAttackType.Projectile;
    public float FireRateOverride = 0.5f;

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

    public float SpeedOverride = 3f;
    public bool AggressiveChase;

    public bool SummonsMinions;
    public int MinionCount;
    public GameObject MinionPrefab;
}
