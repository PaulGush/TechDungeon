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

    [Header("Cleanup")]
    [Tooltip("When the boss dies, instantly kill any remaining minions in the room (they go through normal death — VFX, drops, credits). Disable for setups where minions should outlive the boss.")]
    public bool KillMinionsOnDeath = true;
}
