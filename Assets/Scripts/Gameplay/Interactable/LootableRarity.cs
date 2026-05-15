using System.Collections.Generic;
using UnityEngine;

public static class LootableRarity
{
    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public static Dictionary<Rarity, Color> RarityColors = new Dictionary<Rarity, Color>()
    {
        { Rarity.Common, Color.white },
        { Rarity.Uncommon, Color.green },
        { Rarity.Rare, Color.blue },
        { Rarity.Epic, new Color(1f, 0.5f, 0f) },
        { Rarity.Legendary, new Color(1f, 0.84f, 0f) }
    };

    public static Dictionary<Rarity, float> DamageMultipliers = new Dictionary<Rarity, float>()
    {
        { Rarity.Common, 1.0f },
        { Rarity.Uncommon, 1.25f },
        { Rarity.Rare, 1.6f },
        { Rarity.Epic, 2.0f },
        { Rarity.Legendary, 2.5f }
    };

    public static float GetDamageMultiplier(Rarity rarity)
    {
        return DamageMultipliers.TryGetValue(rarity, out float mult) ? mult : 1f;
    }

    public static Rarity DetermineRarity(float legendaryDropChance, float epicDropChance, float rareDropChance, float uncommonDropChance)
    {
        float roll = Random.Range(0f, 100f);

        if (roll < legendaryDropChance) return Rarity.Legendary;
        if (roll < legendaryDropChance + epicDropChance) return Rarity.Epic;
        if (roll < legendaryDropChance + epicDropChance + rareDropChance) return Rarity.Rare;
        if (roll < legendaryDropChance + epicDropChance + rareDropChance + uncommonDropChance) return Rarity.Uncommon;
        return Rarity.Common;
    }
}
