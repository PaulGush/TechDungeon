using System;
using PlayerObject;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityServiceLocator;

/// <summary>
/// "Death Blossom" style burst: the held weapon vanishes for a beat, a ring of phantom copies
/// appears around the player and each fires one volley outward, then the phantoms fade and the
/// real weapon returns. The volley each phantom fires is the held weapon's own projectile — with
/// the source weapon's pellet count and authored spread, so a shotgun phantom fires a full pellet
/// spread per shot — and inherits whatever ammo type the player has loaded.
/// </summary>
[Serializable]
[MovedFrom(true, sourceClassName: "ProjectileBurstAbilityEffect")]
public class PhantomVolleyAbilityEffect : IAbilityEffect
{
    [Tooltip("Pooled prefab carrying a PhantomWeapon component. One instance is spawned per radial slot.")]
    [SerializeField] private GameObject m_phantomPrefab;

    [Tooltip("Number of phantoms (and volleys) in the radial burst.")]
    [SerializeField, Min(1)] private int m_phantomCount = 8;

    [Tooltip("Distance from the player at which the phantoms appear, in world units. Match the weapon's normal hold distance for a clean read.")]
    [SerializeField, Min(0f)] private float m_radius = 0.65f;

    [Tooltip("Extra flat damage added to each phantom's shot, on top of the projectile's intrinsic damage.")]
    [SerializeField, Min(0)] private int m_bonusDamage = 4;

    [Tooltip("Extra pierce applied to each phantom's shot.")]
    [SerializeField, Min(0)] private int m_bonusPierce;

    public void Execute(in AbilityContext ctx)
    {
        if (ctx.Pool == null || m_phantomPrefab == null) return;

        ServiceLocator.Global.TryGet(out WeaponHolder weaponHolder);
        if (weaponHolder == null || weaponHolder.CurrentWeapon == null) return;

        GameObject weaponGO = weaponHolder.CurrentWeapon;
        WeaponShooting weapon = weaponGO.GetComponent<WeaponShooting>();
        if (weapon == null || weapon.ProjectilePrefab == null) return;

        SpriteRenderer weaponSr = weaponGO.GetComponent<SpriteRenderer>();
        if (weaponSr == null || weaponSr.sprite == null) return;

        // Read pellet count + authored (un-ramped) spread + burst params off the source weapon's
        // settings. We use the authored SpreadDegrees rather than the ramped current value since
        // the player wasn't sustaining fire when the ability cast.
        int pelletsPerShot = 1;
        float spreadDegrees = 0f;
        int burstCount = 1;
        float burstInterval = 0f;
        if (weapon.Settings != null)
        {
            pelletsPerShot = Mathf.Max(1, weapon.Settings.PelletsPerShot);
            spreadDegrees = Mathf.Max(0f, weapon.Settings.SpreadDegrees);
            if (weapon.Settings.FireMode == WeaponFireMode.Burst)
            {
                burstCount = Mathf.Max(1, weapon.Settings.BurstCount);
                burstInterval = Mathf.Max(0f, weapon.Settings.BurstInterval);
            }
        }

        // Pull lifetime from the prefab so the weapon stays hidden exactly long enough — extended
        // to cover the full burst for burst-fire weapons (M16 etc.).
        PhantomWeapon templatePhantom = m_phantomPrefab.GetComponent<PhantomWeapon>();
        float lifetime = templatePhantom != null
            ? templatePhantom.ComputeBurstLifetime(burstCount, burstInterval)
            : 0.4f;
        weapon.SuppressForBurst(lifetime);

        // Resolve the special ammo the player has loaded (a magazine weapon fires the type it
        // loaded; otherwise the selected type if it's non-Standard) — without consuming any.
        // The weapon's intrinsic round (e.g. the RPG missile's explosion) is passed alongside;
        // PhantomWeapon layers the two so a rocket volley with ricochet ammo bounces off walls
        // and still detonates on enemies.
        ServiceLocator.Global.TryGet(out AmmoManager ammoManager);
        AmmoSettings loadedAmmo = null;
        if (ammoManager != null)
        {
            if (weapon.UsesMagazine && weapon.LoadedAmmoType != AmmoType.Standard)
                loadedAmmo = ammoManager.GetSettingsForType(weapon.LoadedAmmoType);
            else if (ammoManager.CurrentAmmoSettings != null && ammoManager.CurrentAmmoSettings.Type != AmmoType.Standard)
                loadedAmmo = ammoManager.CurrentAmmoSettings;
        }
        AmmoSettings intrinsicAmmo = weapon.IntrinsicAmmo;

        Sprite sprite = weaponSr.sprite;
        int sortingLayer = weaponSr.sortingLayerID;
        int sortingOrder = weaponSr.sortingOrder;
        Transform playerTransform = ctx.PlayerTransform;
        GameObject projectilePrefab = weapon.ProjectilePrefab.gameObject;

        // Random offset so consecutive bursts don't produce identical phantom vectors.
        float step = 360f / m_phantomCount;
        float baseAngle = UnityEngine.Random.Range(0f, step);

        for (int i = 0; i < m_phantomCount; i++)
        {
            float angleDeg = baseAngle + i * step;
            float angleRad = angleDeg * Mathf.Deg2Rad;

            Vector3 localOffset = new Vector3(Mathf.Cos(angleRad) * m_radius, Mathf.Sin(angleRad) * m_radius, 0f);
            // The held weapon's world Z rotation equals its aim angle (the holder's -90 offset
            // and Weapon's +90 hold cancel). Apply the same convention here so the phantom sprite
            // sits exactly as the held weapon would if the player aimed in this direction. Flip Y
            // for the upper-left half of the ring, mirroring Weapon.FixedUpdate's flip rule
            // (which keeps the silhouette right-side-up when the holder's Z normalises to < 180).
            float localRotZ = angleDeg;
            float normalized = ((angleDeg % 360f) + 360f) % 360f;
            bool flipY = normalized >= 90f && normalized < 270f;

            GameObject pgo = ctx.Pool.GetPooledObject(m_phantomPrefab);
            if (pgo == null) continue;
            PhantomWeapon phantom = pgo.GetComponent<PhantomWeapon>();
            if (phantom == null) continue;

            phantom.Play(
                playerTransform,
                localOffset,
                localRotZ,
                sprite,
                ctx.TintColor,
                flipY,
                sortingLayer,
                sortingOrder,
                projectilePrefab,
                intrinsicAmmo,
                loadedAmmo,
                m_bonusDamage,
                m_bonusPierce,
                pelletsPerShot,
                spreadDegrees,
                burstCount,
                burstInterval);
        }
    }
}
