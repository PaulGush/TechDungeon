using UnityEngine;
using UnityServiceLocator;

public class ItemPickupEffect : MonoBehaviour, IPickupEffect, IPickupTooltip
{
    [SerializeField] private Item m_item;

    public Item Item => m_item;

    public bool Apply(GameObject collector)
    {
        if (!ServiceLocator.Global.TryGet(out ItemManager itemManager))
            return false;

        itemManager.AddItem(m_item);
        return true;
    }

    public bool TryGetTooltip(out string title, out string body, out string effect)
    {
        if (m_item == null)
        {
            title = body = effect = null;
            return false;
        }

        title = m_item.DisplayName;
        body = m_item.Description;
        effect = m_item.GetEffectString();
        return true;
    }
}
