using UnityEngine;

[CreateAssetMenu(menuName = "Data/Combat/Ammo Settings")]
public class AmmoSettings : ScriptableObject
{
    public AmmoType Type;
    public string DisplayName;
    public Sprite Icon;
    public Color ProjectileColor = Color.white;

    [Header("Piercing")]
    public int BonusPierce;

    [Header("Explosive")]
    public float ExplosionRadius;
    public int ExplosionDamage;
    public GameObject ExplosionEffectPrefab;

    [Header("Chain Lightning")]
    public float ChainRange;
    public int MaxChains;

    [Header("Ricochet")]
    public int MaxBounces;

    [Tooltip("Distance behind the projectile to start the surface-detection raycast — must exceed the projectile's collider extents so the ray originates outside any wall it just embedded in.")]
    public float RicochetRayBackOffset = 2f;

    [Tooltip("Maximum length of the surface-detection raycast. Should comfortably exceed RicochetRayBackOffset.")]
    public float RicochetRayDistance = 4f;

    [Tooltip("How far along the surface normal to reposition the projectile after reflecting, to guarantee it clears the wall.")]
    public float RicochetSurfaceClearance = 0.15f;

    public IAmmoEffect CreateEffect()
    {
        return Type switch
        {
            AmmoType.Explosive => new ExplosiveEffect(ExplosionRadius, ExplosionDamage, ExplosionEffectPrefab),
            AmmoType.ChainLightning => new ChainLightningEffect(ChainRange, MaxChains, this),
            AmmoType.Ricochet => new RicochetEffect(MaxBounces, RicochetRayBackOffset, RicochetRayDistance, RicochetSurfaceClearance),
            _ => null
        };
    }
}
