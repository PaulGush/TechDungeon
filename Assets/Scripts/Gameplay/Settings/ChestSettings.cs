using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChestSettings", menuName = "Data/Loot/Chest Settings")]
public class ChestSettings : ScriptableObject
{
    [Header("Loot Source")]
    public LootDatabase LootDatabase;
    public List<LootItemType> ItemCategories = new();
    public int ItemDropCount = 3;

    [Header("Drop Chances (percentage, must sum to <= 100)")]
    [Range(0f, 100f)] public float LegendaryDropChance = 5;
    [Range(0f, 100f)] public float EpicDropChance = 10;
    [Range(0f, 100f)] public float RareDropChance = 15;
    [Range(0f, 100f)] public float UncommonDropChance = 25;

    [Header("Guaranteed Drops")]
    [Tooltip("Each entry guarantees one drop slot will be a specific prefab.")]
    public List<Lootable> GuaranteedItems = new();

    [Tooltip("Each entry guarantees one drop slot will be a random item of that type from the loot database.")]
    public List<LootItemType> GuaranteedTypes = new();

    public float TotalSpawnTime = 0.5f;
    public float SpawnTimeInterval = 0.05f;

    public int TotalGuaranteedCount => GuaranteedItems.Count + GuaranteedTypes.Count;

    public Lootable[] GetRandomItems()
    {
        if (LootDatabase == null || ItemCategories == null || ItemCategories.Count == 0)
            return System.Array.Empty<Lootable>();

        var droppedItems = new List<Lootable>(ItemDropCount);
        int slotsRemaining = ItemDropCount;

        // Fill guaranteed specific items first
        if (GuaranteedItems != null)
        {
            for (int i = 0; i < GuaranteedItems.Count && slotsRemaining > 0; i++)
            {
                if (GuaranteedItems[i] == null) continue;
                droppedItems.Add(GuaranteedItems[i]);
                slotsRemaining--;
            }
        }

        // Fill guaranteed types from the loot database
        if (GuaranteedTypes != null)
        {
            for (int i = 0; i < GuaranteedTypes.Count && slotsRemaining > 0; i++)
            {
                Lootable match = LootDatabase.GetRandomItemOfType(GuaranteedTypes[i]);
                if (match != null)
                {
                    droppedItems.Add(match);
                    slotsRemaining--;
                }
            }
        }

        // Fill remaining slots randomly from allowed categories
        for (int i = 0; i < slotsRemaining; i++)
        {
            Lootable item = LootDatabase.GetRandomItemFromCategories(ItemCategories);
            if (item == null) continue;
            droppedItems.Add(item);
        }

        return droppedItems.ToArray();
    }
}
