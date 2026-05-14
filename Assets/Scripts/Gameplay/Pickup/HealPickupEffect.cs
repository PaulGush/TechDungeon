using UnityEngine;

public class HealPickupEffect : MonoBehaviour, IPickupEffect, IPickupTooltip
{
    [SerializeField] private int m_healAmount;

    public bool Apply(GameObject collector)
    {
        if (!collector.TryGetComponent(out EntityHealth health))
            return false;

        return health.Heal(m_healAmount);
    }

    public bool CanApply(GameObject collector)
    {
        if (!collector.TryGetComponent(out EntityHealth health)) return false;
        // No prompt at full HP — Heal would no-op anyway and the empty prompt is misleading.
        return health.CurrentHealth < health.MaxHealth;
    }

    public bool TryGetTooltip(out string title, out string body, out string effect)
    {
        title = "Health";
        body = "Restores health.";
        effect = $"+{m_healAmount} HP";
        return true;
    }
}
