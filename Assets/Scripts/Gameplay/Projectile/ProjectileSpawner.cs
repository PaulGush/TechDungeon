using Gameplay.ObjectPool;
using UnityEngine;

// Centralizes the projectile spawn sequence (pool fetch → optional modifiers → optional
// ammo → Initialize) so the four call sites (WeaponShooting, EnemyShooting, BurstAttack,
// ChainLightningEffect) stay consistent and don't drift.
public static class ProjectileSpawner
{
    public static Projectile Spawn(
        ObjectPool pool,
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        int bonusDamage = 0,
        float damageMultiplier = 1f,
        int bonusPierce = 0,
        AmmoSettings ammoSettings = null,
        IAmmoEffect ammoEffect = null,
        Color critTint = default)
    {
        GameObject instance = pool.GetPooledObject(prefab, position, rotation);
        Projectile projectile = instance.GetComponent<Projectile>();

        projectile.SetProjectilePrefab(prefab);
        projectile.SetMutationModifiers(bonusDamage, damageMultiplier, bonusPierce);

        // Apply crit tint before ammo so ApplyAmmoTint sees the lock and skips overwriting the
        // sprite color, preserving crit precedence over ammo color on the sprite (Initialize
        // handles the same precedence for the trail).
        if (critTint.a > 0f)
            projectile.SetCritTint(critTint);

        if (ammoEffect != null)
            projectile.SetAmmoEffect(ammoSettings, ammoEffect);
        else if (ammoSettings != null)
            projectile.SetAmmoModifiers(ammoSettings);

        projectile.Initialize();
        return projectile;
    }
}
