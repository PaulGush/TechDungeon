using System;
using System.Collections.Generic;
using UnityEngine;

public class RoomInstance : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private List<Transform> m_enemySpawnPoints;
    [SerializeField] private Transform m_playerSpawnPoint;

    [Header("Doors")]
    [SerializeField] private List<BulkheadDoor> m_bulkheadDoors;

    [Header("Reward")]
    [SerializeField] private RoomRewardChest m_rewardChest;

    [Header("Camera")]
    [Tooltip("Collider defining the camera bounds for this room. Used by CinemachineConfiner2D.")]
    [SerializeField] private Collider2D m_cameraBounds;

    [Header("Boss")]
    [Tooltip("Designer-placed positions a boss can retreat to (e.g., the four corners of the arena). Used by attack patterns like MissileBarrage.")]
    [SerializeField] private List<Transform> m_bossRetreatPoints = new List<Transform>();

    public IReadOnlyList<Transform> EnemySpawnPoints => m_enemySpawnPoints;
    public IReadOnlyList<BulkheadDoor> BulkheadDoors => m_bulkheadDoors;
    public Transform PlayerSpawnPoint => m_playerSpawnPoint;
    public RoomRewardChest RewardChest => m_rewardChest;
    public Collider2D CameraBounds => m_cameraBounds;
    public IReadOnlyList<Transform> BossRetreatPoints => m_bossRetreatPoints;

    public event Action OnRoomCleared;
    public event Action OnRoomStarted;
    public event Action OnRewardCollected;

    private bool m_isCleared;
    public bool IsCleared => m_isCleared;

    public void StartRoom()
    {
        m_isCleared = false;
        OnRoomStarted?.Invoke();
    }

    public void ClearRoom()
    {
        if (m_isCleared) return;
        m_isCleared = true;
        OnRoomCleared?.Invoke();
    }

    public void CollectReward()
    {
        OnRewardCollected?.Invoke();
    }

    public Transform GetSpawnPoint(int index)
    {
        if (m_enemySpawnPoints == null || m_enemySpawnPoints.Count == 0) return transform;
        return m_enemySpawnPoints[index % m_enemySpawnPoints.Count];
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        if (m_enemySpawnPoints != null)
        {
            foreach (var point in m_enemySpawnPoints)
            {
                if (point != null) Gizmos.DrawWireSphere(point.position, 0.3f);
            }
        }

        Gizmos.color = Color.green;
        if (m_playerSpawnPoint != null)
        {
            Gizmos.DrawWireSphere(m_playerSpawnPoint.position, 0.4f);
        }

        Gizmos.color = Color.cyan;
        if (m_bossRetreatPoints != null)
        {
            foreach (var point in m_bossRetreatPoints)
            {
                if (point != null) Gizmos.DrawWireCube(point.position, Vector3.one * 0.5f);
            }
        }
    }
}