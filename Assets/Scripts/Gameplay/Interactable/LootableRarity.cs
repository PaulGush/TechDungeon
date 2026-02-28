using System.Collections.Generic;
using UnityEngine;

public static class LootableRarity
{
    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic
    }

    public static Dictionary<Rarity, Color> RarityColors = new Dictionary<Rarity, Color>()
    {
        { Rarity.Common, Color.white },
        { Rarity.Uncommon, Color.green },
        { Rarity.Rare, Color.blue },
        { Rarity.Epic, Color.orange }
    };

    public static Rarity DetermineRarity(float epicDropChance, float rareDropChance, float uncommonDropChance)
    {
        float roll = Random.Range(0f, 100f);

        if (roll < epicDropChance) return Rarity.Epic;
        if (roll < epicDropChance + rareDropChance) return Rarity.Rare;
        if (roll < epicDropChance + rareDropChance + uncommonDropChance) return Rarity.Uncommon;
        return Rarity.Common;
    }
}
