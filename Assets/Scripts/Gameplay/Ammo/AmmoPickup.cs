using UnityEngine;
using UnityServiceLocator;

public class AmmoPickup : Lootable
{
    [Header("Ammo")]
    [SerializeField] private AmmoType m_ammoType;
    [SerializeField] private int m_amount = 10;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsSpawning) return;
        if (other.gameObject.layer != GameConstants.Layers.PlayerLayer) return;

        if (ServiceLocator.Global.TryGet(out AmmoManager ammoManager))
        {
            ammoManager.AddAmmo(m_ammoType, m_amount);
        }

        Destroy(gameObject);
    }
}
