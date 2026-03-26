using UnityEngine;

public class HealPickupEffect : MonoBehaviour, IPickupEffect
{
    [SerializeField] private int m_healAmount;

    public bool Apply(GameObject collector)
    {
        if (!collector.TryGetComponent(out EntityHealth health))
            return false;

        return health.Heal(m_healAmount);
    }
}
