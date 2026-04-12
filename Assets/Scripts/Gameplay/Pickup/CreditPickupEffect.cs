using UnityEngine;
using UnityServiceLocator;

public class CreditPickupEffect : MonoBehaviour, IPickupEffect
{
    [SerializeField] private int m_amount = 1;

    public bool Apply(GameObject collector)
    {
        if (!ServiceLocator.Global.TryGet(out CreditManager creditManager))
            return false;

        creditManager.AddCredits(m_amount);
        return true;
    }
}
