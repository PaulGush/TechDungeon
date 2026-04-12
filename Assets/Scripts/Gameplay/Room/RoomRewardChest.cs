using System.Collections.Generic;
using UnityEngine;

public class RoomRewardChest : MonoBehaviour
{
    [SerializeField] private Chest m_chest;
    [SerializeField] private Collider2D m_interactionCollider;
    [SerializeField] private GameObject m_lockedVisual;

    private RoomInstance m_roomInstance;
    private Transform m_playerTransform;
    private RewardIndicator m_indicator;

    public void Initialize(RoomInstance roomInstance, ChestSettings chestSettings, Transform playerTransform)
    {
        m_roomInstance = roomInstance;
        m_playerTransform = playerTransform;

        if (chestSettings != null)
        {
            m_chest.SetSettings(chestSettings);
        }

        Lock();

        m_roomInstance.OnRoomCleared += Unlock;
        m_chest.OnChestOpened += HandleChestOpened;
        m_chest.OnAllItemsCollected += HandleAllItemsCollected;
    }

    private void OnDestroy()
    {
        if (m_roomInstance != null)
        {
            m_roomInstance.OnRoomCleared -= Unlock;
        }

        if (m_chest != null)
        {
            m_chest.OnChestOpened -= HandleChestOpened;
            m_chest.OnAllItemsCollected -= HandleAllItemsCollected;
        }

        if (m_indicator != null)
        {
            Destroy(m_indicator.gameObject);
        }
    }

    private void HandleChestOpened(List<GameObject> items)
    {
        if (items.Count == 0 || m_playerTransform == null) return;

        GameObject indicatorObj = new GameObject("RewardIndicator");
        m_indicator = indicatorObj.AddComponent<RewardIndicator>();
        m_indicator.Initialize(m_playerTransform, items);
    }

    private void HandleAllItemsCollected()
    {
        m_roomInstance?.CollectReward();

        if (m_indicator != null)
        {
            Destroy(m_indicator.gameObject);
            m_indicator = null;
        }
    }

    private void Lock()
    {
        m_chest.SetLockState(true);

        if (m_interactionCollider != null)
            m_interactionCollider.enabled = false;

        if (m_lockedVisual != null)
            m_lockedVisual.SetActive(true);
    }

    private void Unlock()
    {
        m_chest.SetLockState(false);

        if (m_interactionCollider != null)
            m_interactionCollider.enabled = true;

        if (m_lockedVisual != null)
            m_lockedVisual.SetActive(false);
    }
}
