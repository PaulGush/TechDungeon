using UnityEngine;

[CreateAssetMenu(menuName = "Data/Room/Boss Room Settings")]
public class BossRoomSettings : RoomSettings
{
    [Header("Rewards")]
    [Tooltip("Chest settings dropped specifically by this boss on defeat.")]
    public ChestSettings BossRewardChest;
}
