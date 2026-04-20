using UnityEngine;

public enum WeaponFireMode
{
    SemiAuto,
    FullAuto,
    Burst,
    Charge
}

[CreateAssetMenu(menuName = "Data/Combat/Weapon Settings")]
public class WeaponSettings : ScriptableObject
{
    [Header("Fire Mode")]
    public WeaponFireMode FireMode = WeaponFireMode.SemiAuto;

    [Header("Timing")]
    [Tooltip("Seconds between shots (SemiAuto/FullAuto), between bursts (Burst), or between charge cycles (Charge).")]
    public float Cooldown = 0.25f;

    [Header("Accuracy")]
    [Tooltip("Maximum random deviation applied to each shot's direction in degrees (half-angle).")]
    public float SpreadDegrees = 0f;

    [Tooltip("Projectiles spawned per shot. Greater than 1 for shotgun-style pellets.")]
    public int PelletsPerShot = 1;

    [Header("Burst Mode")]
    [Tooltip("Shots fired per burst. Only used when FireMode is Burst.")]
    public int BurstCount = 3;

    [Tooltip("Seconds between shots within a single burst. Only used when FireMode is Burst.")]
    public float BurstInterval = 0.06f;

    [Header("Charge Mode")]
    [Tooltip("Seconds held required before the weapon is allowed to fire. Only used when FireMode is Charge.")]
    public float MinChargeSeconds = 0.25f;

    [Tooltip("Seconds held required to reach full damage. Only used when FireMode is Charge.")]
    public float MaxChargeSeconds = 1f;

    [Tooltip("Damage multiplier applied at minimum charge. Scales linearly to 1.0 at full charge.")]
    [Range(0.1f, 1f)] public float MinChargeDamageMultiplier = 0.5f;

    [Header("Kickback")]
    [Tooltip("How far the weapon is pushed back along its local -Y (toward the player) at the moment of fire. Zero disables.")]
    public float KickbackDistance = 0f;

    [Tooltip("Seconds for the weapon to return from its peak kickback back to its rest position. Zero disables.")]
    public float KickbackDuration = 0.08f;

    [Header("Intrinsic Ammo")]
    [Tooltip("Ammo effect baked into this weapon — applied to every shot when the player has no override ammo loaded. Unlike player ammo, this is never consumed. Leave empty for weapons that only use whatever ammo the player has equipped.")]
    public AmmoSettings IntrinsicAmmo;
}
