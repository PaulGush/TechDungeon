using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Settings/Shop Settings")]
public class ShopSettings : ScriptableObject
{
    [Header("Item Pools")]
    public List<Lootable> WeaponPool;
    public List<Lootable> MutationPool;
    public List<Lootable> ConsumablePool;

    [Header("Pricing")]
    public PriceRange CommonPrice = new(10, 20);
    public PriceRange UncommonPrice = new(20, 40);
    public PriceRange RarePrice = new(40, 70);
    public PriceRange EpicPrice = new(70, 120);
    public PriceRange LegendaryPrice = new(120, 200);

    [Header("Rarity Chances")]
    [Range(0, 100)] public float LegendaryDropChance = 2f;
    [Range(0, 100)] public float EpicDropChance = 8f;
    [Range(0, 100)] public float RareDropChance = 20f;
    [Range(0, 100)] public float UncommonDropChance = 35f;

    [Header("Steal Punishment")]
    public List<EnemyWave> StealWaves;

    [Serializable]
    public struct PriceRange
    {
        public int Min;
        public int Max;

        public PriceRange(int min, int max)
        {
            Min = min;
            Max = max;
        }

        public int GetPrice() => UnityEngine.Random.Range(Min, Max + 1);
    }

    public int GetPriceForRarity(LootableRarity.Rarity rarity)
    {
        return rarity switch
        {
            LootableRarity.Rarity.Common => CommonPrice.GetPrice(),
            LootableRarity.Rarity.Uncommon => UncommonPrice.GetPrice(),
            LootableRarity.Rarity.Rare => RarePrice.GetPrice(),
            LootableRarity.Rarity.Epic => EpicPrice.GetPrice(),
            LootableRarity.Rarity.Legendary => LegendaryPrice.GetPrice(),
            _ => CommonPrice.GetPrice()
        };
    }

    public LootableRarity.Rarity GetRandomRarity()
    {
        return LootableRarity.DetermineRarity(LegendaryDropChance, EpicDropChance, RareDropChance, UncommonDropChance);
    }

    public Lootable GetRandomFromPool(List<Lootable> pool)
    {
        if (pool == null || pool.Count == 0) return null;
        return pool[UnityEngine.Random.Range(0, pool.Count)];
    }
}
