using System.Collections.Generic;
using UnityEngine;

public class LootableRarity
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
}