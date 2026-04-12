using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Room/Room Settings")]
public class RoomSettings : ScriptableObject
{
    [Header("Room Info")]
    public RoomType RoomType;
    public RewardType RewardType;
    public Sprite RoomIcon;

    [Header("Room Prefabs")]
    [Tooltip("Possible room layouts for this configuration. One is chosen at random.")]
    public List<RoomInstance> RoomPrefabs;

    [Header("Combat (only used for Combat/Boss rooms)")]
    public List<EnemyWave> EnemyWaves;

    public RoomInstance GetRandomRoomPrefab()
    {
        if (RoomPrefabs == null || RoomPrefabs.Count == 0) return null;
        return RoomPrefabs[Random.Range(0, RoomPrefabs.Count)];
    }
}
