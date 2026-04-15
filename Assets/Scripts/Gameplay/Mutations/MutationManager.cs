using System;
using System.Collections.Generic;
using UnityEngine;
using UnityServiceLocator;

public class MutationManager : MonoBehaviour
{
    // Mutation Value for percent-based mutations is authored as 0–100 (designer-friendly), so divide here.
    private const float PercentToMultiplier = 0.01f;

    [SerializeField] private EntityHealth m_health;

    private readonly List<Mutation> m_mutations = new();

    // Aggregate cache, indexed by MutationType. Updated incrementally on AddMutation/Reset
    // so the per-shot getters are O(1) instead of scanning the full mutation list every time.
    private readonly float[] m_aggregateByType = new float[Enum.GetValues(typeof(MutationType)).Length];

    public IReadOnlyList<Mutation> Mutations => m_mutations;
    public Action<Mutation> OnMutationAdded;

    private void Awake()
    {
        ServiceLocator.Global.Register(this);
    }

    public void Reset()
    {
        m_mutations.Clear();
        Array.Clear(m_aggregateByType, 0, m_aggregateByType.Length);
        m_health.ResetToBase();
    }

    public void AddMutation(Mutation mutation)
    {
        m_mutations.Add(mutation);
        m_aggregateByType[(int)mutation.Type] += mutation.Value;

        switch (mutation.Type)
        {
            case MutationType.MaxHealth:
                m_health.IncreaseMaxHealth(Mathf.RoundToInt(mutation.Value));
                break;
            case MutationType.Armor:
                m_health.IncreaseArmor(Mathf.RoundToInt(mutation.Value));
                break;
        }

        OnMutationAdded?.Invoke(mutation);
    }

    public int GetFlatDamageBonus() => Mathf.RoundToInt(m_aggregateByType[(int)MutationType.FlatDamage]);

    public float GetDamageMultiplier() => 1f + m_aggregateByType[(int)MutationType.PercentDamage] * PercentToMultiplier;

    public int GetBonusPierce() => Mathf.RoundToInt(m_aggregateByType[(int)MutationType.Pierce]);

    public float GetSpeedMultiplier() => 1f + m_aggregateByType[(int)MutationType.PercentSpeed] * PercentToMultiplier;

    public float GetAmmoEfficiency() => m_aggregateByType[(int)MutationType.AmmoEfficiency];
}
