using System;
using System.Collections.Generic;
using UnityEngine;
using UnityServiceLocator;

public class ItemManager : MonoBehaviour
{
    // Item Value for percent-based items is authored as 0–100 (designer-friendly), so divide here.
    private const float PercentToMultiplier = 0.01f;

    [SerializeField] private EntityHealth m_health;

    private readonly List<Item> m_items = new();

    // Aggregate cache, indexed by ItemType. Updated incrementally on AddItem/Reset
    // so the per-shot getters are O(1) instead of scanning the full item list every time.
    private readonly float[] m_aggregateByType = new float[Enum.GetValues(typeof(ItemType)).Length];

    public IReadOnlyList<Item> Items => m_items;
    public Action<Item> OnItemAdded;

    private void Awake()
    {
        ServiceLocator.Global.Register(this);
    }

    public void Reset()
    {
        m_items.Clear();
        Array.Clear(m_aggregateByType, 0, m_aggregateByType.Length);
        m_health.ResetToBase();
    }

    public void AddItem(Item item)
    {
        m_items.Add(item);
        m_aggregateByType[(int)item.Type] += item.Value;

        switch (item.Type)
        {
            case ItemType.MaxHealth:
                m_health.IncreaseMaxHealth(Mathf.RoundToInt(item.Value));
                break;
            case ItemType.Armor:
                m_health.IncreaseArmor(Mathf.RoundToInt(item.Value));
                break;
        }

        OnItemAdded?.Invoke(item);
    }

    public int GetFlatDamageBonus() => Mathf.RoundToInt(m_aggregateByType[(int)ItemType.FlatDamage]);

    public float GetDamageMultiplier() => 1f + m_aggregateByType[(int)ItemType.PercentDamage] * PercentToMultiplier;

    public int GetBonusPierce() => Mathf.RoundToInt(m_aggregateByType[(int)ItemType.Pierce]);

    public float GetSpeedMultiplier() => 1f + m_aggregateByType[(int)ItemType.PercentSpeed] * PercentToMultiplier;

    public float GetAmmoEfficiency() => m_aggregateByType[(int)ItemType.AmmoEfficiency];
}
