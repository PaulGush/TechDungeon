using UnityEngine;
using UnityEngine.Timeline;

[CreateAssetMenu(menuName = "Data/Room/Boss Room Settings")]
public class BossRoomSettings : RoomSettings
{
    [Header("Rewards")]
    [Tooltip("Chest settings dropped specifically by this boss on defeat.")]
    public ChestSettings BossRewardChest;

    [Header("Cinematic")]
    [Tooltip("Timeline played before the boss fight begins. Leave empty to skip.")]
    public TimelineAsset IntroCinematic;

    [Tooltip("Spawn the boss before the cinematic so it's visible during the intro.")]
    public bool PreSpawnBoss;
}
