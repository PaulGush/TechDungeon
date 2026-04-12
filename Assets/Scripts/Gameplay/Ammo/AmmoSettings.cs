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

    public IAmmoEffect CreateEffect()
    {
        return Type switch
        {
            AmmoType.Explosive => new ExplosiveEffect(ExplosionRadius, ExplosionDamage, ExplosionEffectPrefab),
            AmmoType.ChainLightning => new ChainLightningEffect(ChainRange, MaxChains, this),
            AmmoType.Ricochet => new RicochetEffect(MaxBounces),
            _ => null
        };
    }
}
