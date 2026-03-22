using System;
using System.Collections.Generic;
using UnityEngine;
using UnityServiceLocator;

public class MutationManager : MonoBehaviour
{
    [SerializeField] private EntityHealth m_health;

    private readonly List<Mutation> m_mutations = new();

    public IReadOnlyList<Mutation> Mutations => m_mutations;
    public Action<Mutation> OnMutationAdded;

    private void Awake()
    {
        ServiceLocator.Global.Register(this);
    }

    public void AddMutation(Mutation mutation)
    {
        m_mutations.Add(mutation);

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

    public int GetFlatDamageBonus()
    {
        int total = 0;
        foreach (Mutation m in m_mutations)
            if (m.Type == MutationType.FlatDamage)
                total += Mathf.RoundToInt(m.Value);
        return total;
    }

    public float GetDamageMultiplier()
    {
        float total = 1f;
        foreach (Mutation m in m_mutations)
            if (m.Type == MutationType.PercentDamage)
                total += m.Value / 100f;
        return total;
    }

    public int GetBonusPierce()
    {
        int total = 0;
        foreach (Mutation m in m_mutations)
            if (m.Type == MutationType.Pierce)
                total += Mathf.RoundToInt(m.Value);
        return total;
    }

    public float GetSpeedMultiplier()
    {
        float total = 1f;
        foreach (Mutation m in m_mutations)
            if (m.Type == MutationType.PercentSpeed)
                total += m.Value / 100f;
        return total;
    }

    public float GetAmmoEfficiency()
    {
        float total = 0f;
        foreach (Mutation m in m_mutations)
            if (m.Type == MutationType.AmmoEfficiency)
                total += m.Value;
        return total;
    }
}
