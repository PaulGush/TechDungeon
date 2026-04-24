using UnityEngine;
using UnityServiceLocator;

public class ItemPickupEffect : MonoBehaviour, IPickupEffect
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
}
