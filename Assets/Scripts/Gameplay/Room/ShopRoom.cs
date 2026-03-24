using System.Collections.Generic;
using UnityEngine;
using UnityServiceLocator;

public class ShopRoom : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private ShopSettings m_settings;

    [Header("Pedestals")]
    [SerializeField] private List<ShopPedestal> m_pedestals;

    [Header("Spawn Indicators")]
    [SerializeField] private GameObject m_spawnIndicatorPrefab;
    [SerializeField] private float m_spawnIndicatorDuration = 0.8f;

    private RoomInstance m_roomInstance;
    private RoomEncounter m_stealEncounter;
    private bool m_hasStolen;

    private void Start()
    {
        m_roomInstance = GetComponent<RoomInstance>();
        InitializePedestals();
    }

    private void InitializePedestals()
    {
        List<Lootable>[] pools = { m_settings.WeaponPool, m_settings.MutationPool, m_settings.ConsumablePool };

        for (int i = 0; i < m_pedestals.Count; i++)
        {
            List<Lootable> pool = pools[i % pools.Length];
            Lootable item = m_settings.GetRandomFromPool(pool);

            if (item == null) continue;

            LootableRarity.Rarity rarity = m_settings.GetRandomRarity();
            int price = m_settings.GetPriceForRarity(rarity);

            m_pedestals[i].Initialize(item, rarity, price, this);
        }
    }

    public void HandleSteal(ShopPedestal pedestal)
    {
        if (m_hasStolen) return;
        if (m_settings.StealWaves == null || m_settings.StealWaves.Count == 0) return;

        m_hasStolen = true;

        foreach (BulkheadDoor door in m_roomInstance.BulkheadDoors)
        {
            if (door != null) door.Lock();
        }

        m_roomInstance.OnRoomCleared += OnStealEncounterCleared;
        m_roomInstance.StartRoom();

        m_stealEncounter = gameObject.AddComponent<RoomEncounter>();
        m_stealEncounter.Initialize(m_roomInstance, m_settings.StealWaves, m_spawnIndicatorPrefab, m_spawnIndicatorDuration);
        m_stealEncounter.StartEncounter();
    }

    private void OnStealEncounterCleared()
    {
        m_roomInstance.OnRoomCleared -= OnStealEncounterCleared;

        foreach (BulkheadDoor door in m_roomInstance.BulkheadDoors)
        {
            if (door != null) door.Unlock();
        }
    }
}
