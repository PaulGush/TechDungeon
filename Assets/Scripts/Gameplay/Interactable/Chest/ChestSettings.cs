using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChestSettings", menuName = "Interactables/Chest Settings")]
public class ChestSettings : ScriptableObject
{
    public List<Lootable> Items;
    public int ItemDropCount = 3;
    public float CommonDropChance = 50;
    public float UncommonDropChance = 30;
    public float RareDropChance = 15;
    public float EpicDropChance = 5;
    
    public Lootable[] GetRandomItems()
    {
        Lootable[] droppedItems = new Lootable[ItemDropCount];

        for(int i = 0; i < ItemDropCount; i++)
        {
            droppedItems[i] = Items[Random.Range(0, Items.Count)];

            int roll = Random.Range(0, 100);
            
            LootableRarity.Rarity selectedRarity = LootableRarity.Rarity.Common;

            selectedRarity = roll switch
            {
                _ when roll < EpicDropChance => LootableRarity.Rarity.Epic,
                _ when roll < EpicDropChance + RareDropChance => LootableRarity.Rarity.Rare,
                _ when roll < EpicDropChance + RareDropChance + UncommonDropChance => LootableRarity.Rarity.Uncommon,
                _ => LootableRarity.Rarity.Common
            };

            droppedItems[i].ChangeRarity(selectedRarity);
        }
        
        return droppedItems;
    }
}