using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChestSettings", menuName = "Interactables/Chest Settings")]
public class ChestSettings : ScriptableObject
{
    public List<Lootable> Items;
    public int ItemDropCount = 3;

    [Header("Drop Chances (percentage, must sum to <= 100)")]
    [Range(0f, 100f)] public float LegendaryDropChance = 5;
    [Range(0f, 100f)] public float EpicDropChance = 10;
    [Range(0f, 100f)] public float RareDropChance = 15;
    [Range(0f, 100f)] public float UncommonDropChance = 25;

    [Header("Guaranteed Drops")]
    [Tooltip("Each entry guarantees one drop slot will be a specific prefab.")]
    public List<Lootable> GuaranteedItems = new();

    [Tooltip("Each entry guarantees one drop slot will be a random item of that type from the Items pool.")]
    public List<LootItemType> GuaranteedTypes = new();

    public float TotalSpawnTime = 0.5f;
    public float SpawnTimeInterval = 0.05f;

    public int TotalGuaranteedCount => GuaranteedItems.Count + GuaranteedTypes.Count;

    public Lootable[] GetRandomItems()
    {
        if (Items == null || Items.Count == 0)
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

        // Fill guaranteed types from the Items pool
        if (GuaranteedTypes != null)
        {
            for (int i = 0; i < GuaranteedTypes.Count && slotsRemaining > 0; i++)
            {
                Lootable match = GetRandomItemOfType(GuaranteedTypes[i]);
                if (match != null)
                {
                    droppedItems.Add(match);
                    slotsRemaining--;
                }
            }
        }

        // Fill remaining slots randomly
        for (int i = 0; i < slotsRemaining; i++)
        {
            Lootable item = Items[Random.Range(0, Items.Count)];
            if (item == null) continue;
            droppedItems.Add(item);
        }

        return droppedItems.ToArray();
    }

    private Lootable GetRandomItemOfType(LootItemType type)
    {
        var matches = new List<Lootable>();
        foreach (Lootable item in Items)
        {
            if (item != null && item.ItemType == type)
                matches.Add(item);
        }

        if (matches.Count == 0) return null;
        return matches[Random.Range(0, matches.Count)];
    }
}
