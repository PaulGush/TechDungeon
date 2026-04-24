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
    [Header("Display")]
    [Tooltip("Shown in the weapon HUD. Prefab-name fallback kicks in only when this is left blank.")]
    public string DisplayName;

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

    [Tooltip("Seconds of continuous attack-held input required to ramp spread from SpreadDegrees up to MaxSpreadDegrees. Zero (or MaxSpreadDegrees not greater than SpreadDegrees) disables the ramp; the weapon uses SpreadDegrees as a fixed value. Intended to incentivize short controlled bursts on full-auto weapons.")]
    public float SpreadRampDuration = 0f;

    [Tooltip("Spread in degrees that sustained fire ramps toward. Only takes effect when SpreadRampDuration > 0 and this value exceeds SpreadDegrees. Spread resets to SpreadDegrees when attack input is released or when a reload begins.")]
    public float MaxSpreadDegrees = 0f;

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

    [Header("Magazine")]
    [Tooltip("Shots per magazine. Zero or negative disables reload (infinite mag).")]
    public int MagazineSize = 0;

    [Tooltip("Seconds the reload takes to complete. Ignored when MagazineSize is non-positive.")]
    public float ReloadDuration = 1.2f;

    [Header("Kickback")]
    [Tooltip("How far the weapon is pushed back along its local -Y (toward the player) at the moment of fire. Zero disables.")]
    public float KickbackDistance = 0f;

    [Tooltip("Seconds for the weapon to return from its peak kickback back to its rest position. Zero disables.")]
    public float KickbackDuration = 0.08f;

    [Header("Recoil")]
    [Tooltip("Peak aim rotation (in degrees) applied to the weapon the moment it fires, decaying back to zero over RecoilDuration. Visually tips the muzzle away from the aim line so sustained fire reads as 'kicking'. Zero disables.")]
    public float RecoilDegrees = 0f;

    [Tooltip("Seconds for the recoil rotation to decay from its peak back to zero. Zero disables.")]
    public float RecoilDuration = 0.1f;

    [Header("Intrinsic Ammo")]
    [Tooltip("Ammo effect baked into this weapon — applied to every shot when the player has no override ammo loaded. Unlike player ammo, this is never consumed. Leave empty for weapons that only use whatever ammo the player has equipped.")]
    public AmmoSettings IntrinsicAmmo;
}
