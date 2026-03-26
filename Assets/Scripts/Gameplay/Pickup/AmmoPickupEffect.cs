using UnityEngine;
using UnityServiceLocator;

public class AmmoPickupEffect : MonoBehaviour, IPickupEffect
{
    [SerializeField] private AmmoType m_ammoType;
    [SerializeField] private int m_amount = 10;

    public bool Apply(GameObject collector)
    {
        if (!ServiceLocator.Global.TryGet(out AmmoManager ammoManager))
            return false;

        ammoManager.AddAmmo(m_ammoType, m_amount);
        return true;
    }
}
