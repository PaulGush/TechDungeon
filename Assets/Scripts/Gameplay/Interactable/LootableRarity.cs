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

    public static Rarity DetermineRarity(Lootable lootable, float epicDropChance, float rareDropChance, float uncommonDropChance)
    {
        int roll = Random.Range(0, 100);
            
        Debug.Log($"Roll: {roll}");

        Rarity selectedRarity = roll switch
        {
            _ when roll >= epicDropChance => Rarity.Epic,
            _ when roll >= rareDropChance => Rarity.Rare,
            _ when roll >= uncommonDropChance => Rarity.Uncommon,
            _ => Rarity.Common
        };

        return selectedRarity;
    }
}