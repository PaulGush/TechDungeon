using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Room/Floor Settings")]
public class FloorSettings : ScriptableObject
{
    [Header("Floor Structure")]
    [Tooltip("Total number of rooms in this floor, including the boss.")]
    public int TotalRoomCount = 10;

    [Tooltip("Place a shop room every N rooms. Set to 0 to disable shops.")]
    public int ShopInterval = 4;

    [Header("Room Pools")]
    public List<RoomSettings> CombatRoomPool;
    public List<RoomSettings> ShopRoomPool;
    public BossRoomSettings BossRoom;

    [Header("Rewards")]
    [Tooltip("Reward types that can be offered on doors for combat rooms.")]
    public List<RewardType> RewardOptions;

    [Tooltip("Maps each reward type to the chest settings used when that reward is chosen.")]
    public List<RewardChestMapping> RewardChestMappings;

    public ChestSettings GetChestSettingsForReward(RewardType rewardType)
    {
        if (RewardChestMappings == null) return null;

        foreach (var mapping in RewardChestMappings)
        {
            if (mapping.RewardType == rewardType)
                return mapping.ChestSettings;
        }

        return null;
    }

    public List<RoomSlot> GenerateFloorSequence()
    {
        var sequence = new List<RoomSlot>(TotalRoomCount);

        for (int i = 0; i < TotalRoomCount; i++)
        {
            sequence.Add(default);
        }

        // Boss is always last
        sequence[TotalRoomCount - 1] = new RoomSlot
        {
            Settings = BossRoom,
            RoomType = RoomType.Boss
        };

        // Place shops at intervals, ensuring they're never adjacent
        if (ShopInterval > 0 && ShopRoomPool != null && ShopRoomPool.Count > 0)
        {
            for (int i = ShopInterval - 1; i < TotalRoomCount - 1; i += ShopInterval)
            {
                sequence[i] = new RoomSlot
                {
                    Settings = ShopRoomPool[UnityEngine.Random.Range(0, ShopRoomPool.Count)],
                    RoomType = RoomType.Shop
                };
            }
        }

        // Fill remaining slots with random combat rooms
        for (int i = 0; i < TotalRoomCount - 1; i++)
        {
            if (sequence[i].Settings != null) continue;

            sequence[i] = new RoomSlot
            {
                Settings = CombatRoomPool[UnityEngine.Random.Range(0, CombatRoomPool.Count)],
                RoomType = RoomType.Combat
            };
        }

        return sequence;
    }
}

[Serializable]
public struct RoomSlot
{
    public RoomSettings Settings;
    public RoomType RoomType;
}

[Serializable]
public struct RewardChestMapping
{
    public RewardType RewardType;
    public ChestSettings ChestSettings;
}
