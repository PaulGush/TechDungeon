using UnityEngine;
using UnityServiceLocator;

public class AmmoPickupEffect : MonoBehaviour, IPickupEffect, IPickupTooltip
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

    public bool TryGetTooltip(out string title, out string body, out string effect)
    {
        string typeName = m_ammoType.ToString();
        if (ServiceLocator.Global.TryGet(out AmmoManager ammoManager))
        {
            AmmoSettings settings = ammoManager.GetSettingsForType(m_ammoType);
            if (settings != null && !string.IsNullOrWhiteSpace(settings.DisplayName))
                typeName = settings.DisplayName;
        }

        title = $"{typeName} Ammo";
        body = "Special ammunition for the equipped weapon.";
        effect = $"+{m_amount} rounds";
        return true;
    }
}
