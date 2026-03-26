using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[CreateAssetMenu(fileName = "LootDatabase", menuName = "Data/Loot/Loot Database")]
public class LootDatabase : ScriptableObject
{
    [Serializable]
    public class LootCategory
    {
        public LootItemType Type;
        public List<Lootable> Items = new();
    }

    [SerializeField] private List<LootCategory> m_categories = new();

    public List<Lootable> GetItemsOfType(LootItemType type)
    {
        foreach (var category in m_categories)
        {
            if (category.Type == type)
                return category.Items;
        }
        return new List<Lootable>();
    }

    public Lootable GetRandomItemOfType(LootItemType type)
    {
        var items = GetItemsOfType(type);
        if (items.Count == 0) return null;
        return items[Random.Range(0, items.Count)];
    }

    public Lootable GetRandomItemFromCategories(List<LootItemType> categories)
    {
        if (categories == null || categories.Count == 0) return null;
        var type = categories[Random.Range(0, categories.Count)];
        return GetRandomItemOfType(type);
    }

    public bool HasItemsOfType(LootItemType type)
    {
        return GetItemsOfType(type).Count > 0;
    }
}
