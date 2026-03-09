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

    public float TotalSpawnTime = 0.5f;
    public float SpawnTimeInterval = 0.05f;

    public Lootable[] GetRandomItems()
    {
        if (Items == null || Items.Count == 0)
            return System.Array.Empty<Lootable>();

        var droppedItems = new List<Lootable>(ItemDropCount);

        for (int i = 0; i < ItemDropCount; i++)
        {
            Lootable item = Items[Random.Range(0, Items.Count)];
            if (item == null) continue;
            droppedItems.Add(item);
        }

        return droppedItems.ToArray();
    }
}
