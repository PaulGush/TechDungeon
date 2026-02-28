using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChestSettings", menuName = "Interactables/Chest Settings")]
public class ChestSettings : ScriptableObject
{
    public List<Lootable> Items;
    public int ItemDropCount = 3;

    [Header("Drop Chances (percentage, must sum to <= 100)")]
    public float EpicDropChance = 10;
    public float RareDropChance = 15;
    public float UncommonDropChance = 25;

    public float TotalSpawnTime = 0.5f;
    public float SpawnTimeInterval = 0.05f;

    public Lootable[] GetRandomItems()
    {
        Lootable[] droppedItems = new Lootable[ItemDropCount];

        for(int i = 0; i < ItemDropCount; i++)
        {
            droppedItems[i] = Items[Random.Range(0, Items.Count)];
        }

        return droppedItems;
    }
}
