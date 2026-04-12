using System.Collections.Generic;
using UnityEngine;
using UnityServiceLocator;

public class FloorManager : MonoBehaviour
{
    [SerializeField] private FloorSettings m_floorSettings;

    private List<RoomSlot> m_floorSequence;
    private int m_currentRoomIndex;

    public int CurrentRoomIndex => m_currentRoomIndex;
    public int TotalRooms => m_floorSequence?.Count ?? 0;
    public bool IsLastRoom => m_currentRoomIndex >= TotalRooms - 1;

    private void Awake()
    {
        ServiceLocator.Global.Register(this);
    }

    public void GenerateFloor()
    {
        m_floorSequence = m_floorSettings.GenerateFloorSequence();
        m_currentRoomIndex = -1;
    }

    public RoomSlot GetNextRoomSlot()
    {
        int nextIndex = m_currentRoomIndex + 1;
        if (m_floorSequence == null || nextIndex >= m_floorSequence.Count)
            return default;

        return m_floorSequence[nextIndex];
    }

    public void AdvanceRoom()
    {
        m_currentRoomIndex++;
    }

    public RewardType GetRandomRewardType()
    {
        var options = m_floorSettings.RewardOptions;
        if (options == null || options.Count == 0)
            return RewardType.Credits;

        return options[Random.Range(0, options.Count)];
    }

    public RewardType GetRandomRewardTypeExcluding(HashSet<RewardType> exclude)
    {
        var options = m_floorSettings.RewardOptions;
        if (options == null || options.Count == 0)
            return RewardType.Credits;

        var filtered = new List<RewardType>();
        foreach (var option in options)
        {
            if (!exclude.Contains(option))
                filtered.Add(option);
        }

        if (filtered.Count == 0)
            return options[Random.Range(0, options.Count)];

        return filtered[Random.Range(0, filtered.Count)];
    }

    public ChestSettings GetChestSettingsForReward(RewardType rewardType)
    {
        return m_floorSettings.GetChestSettingsForReward(rewardType);
    }

    public void Reset()
    {
        GenerateFloor();
    }
}
