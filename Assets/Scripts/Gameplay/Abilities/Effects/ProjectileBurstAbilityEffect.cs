using System;
using PlayerObject;
using UnityEngine;
using UnityServiceLocator;

/// <summary>
/// "Death Blossom" style burst: the held weapon vanishes for a beat, a ring of phantom copies
/// appears around the player and each fires one shot outward, then the phantoms fade and the
/// real weapon returns. The shot each phantom fires is the held weapon's own projectile, with
/// whatever ammo type the player has loaded — so the burst inherits the weapon's character.
/// </summary>
[Serializable]
public class ProjectileBurstAbilityEffect : IAbilityEffect
{
    [Tooltip("Pooled prefab carrying a PhantomWeapon component. One instance is spawned per radial slot.")]
    [SerializeField] private GameObject m_phantomPrefab;

    [Tooltip("Number of phantoms (and shots) in the radial burst.")]
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

        // Pull lifetime from the prefab so the weapon stays hidden exactly long enough.
        PhantomWeapon templatePhantom = m_phantomPrefab.GetComponent<PhantomWeapon>();
        float lifetime = templatePhantom != null ? templatePhantom.TotalLifetime : 0.4f;
        weapon.SuppressForBurst(lifetime);

        // Resolve the special ammo the player has loaded (a magazine weapon fires the type it
        // loaded; otherwise the selected type if it's non-Standard) — without consuming any.
        // The weapon's intrinsic round (e.g. the RPG missile's explosion) is passed alongside;
        // PhantomWeapon layers the two so a rocket burst with ricochet ammo bounces off walls
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
                m_bonusPierce);
        }
    }
}
