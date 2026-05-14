using UnityEngine;
using UnityServiceLocator;

public class CreditPickupEffect : MonoBehaviour, IPickupEffect, IPickupTooltip
{
    [SerializeField] private int m_amount = 1;

    public bool Apply(GameObject collector)
    {
        if (!ServiceLocator.Global.TryGet(out CreditManager creditManager))
            return false;

        creditManager.AddCredits(m_amount);
        return true;
    }

    public bool TryGetTooltip(out string title, out string body, out string effect)
    {
        title = "Credits";
        body = "Currency for the shop.";
        effect = $"+{m_amount} CR";
        return true;
    }
}
