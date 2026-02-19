using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChestSettings", menuName = "Interactables/Chest Settings")]
public class ChestSettings : ScriptableObject
{
    public List<Lootable> Items;
    public int ItemDropCount = 3;
    public float UncommonDropChance = 50;
    public float RareDropChance = 75;
    public float EpicDropChance = 90;
    
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