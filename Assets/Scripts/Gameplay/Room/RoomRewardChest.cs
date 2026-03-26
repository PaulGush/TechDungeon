using UnityEngine;

public class RoomRewardChest : MonoBehaviour
{
    [SerializeField] private Chest m_chest;
    [SerializeField] private Collider2D m_interactionCollider;
    [SerializeField] private GameObject m_lockedVisual;

    private RoomInstance m_roomInstance;
    private bool m_isLocked = true;

    public void Initialize(RoomInstance roomInstance, ChestSettings chestSettings)
    {
        m_roomInstance = roomInstance;

        if (chestSettings != null)
        {
            m_chest.SetSettings(chestSettings);
        }

        Lock();

        m_roomInstance.OnRoomCleared += Unlock;
    }

    private void OnDestroy()
    {
        if (m_roomInstance != null)
        {
            m_roomInstance.OnRoomCleared -= Unlock;
        }
    }

    private void Lock()
    {
        m_isLocked = true;

        if (m_interactionCollider != null)
            m_interactionCollider.enabled = false;

        if (m_lockedVisual != null)
            m_lockedVisual.SetActive(true);
    }

    private void Unlock()
    {
        m_isLocked = false;

        if (m_interactionCollider != null)
            m_interactionCollider.enabled = true;

        if (m_lockedVisual != null)
            m_lockedVisual.SetActive(false);
    }
}
