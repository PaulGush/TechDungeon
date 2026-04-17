using System;
using System.Collections;
using System.Collections.Generic;
using Input;
using Unity.Cinemachine;
using UnityEngine;
using UnityServiceLocator;

public class RoomManager : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform m_player;
    [SerializeField] private InputReader m_inputReader;

    [Header("Transition")]
    [SerializeField] private CanvasGroup m_fadeCanvas;
    [SerializeField] private float m_fadeDuration = 0.5f;

    [Header("Room Spawn")]
    [SerializeField] private Transform m_roomParent;

    [Header("Spawn Indicators")]
    [SerializeField] private GameObject m_spawnIndicatorPrefab;
    [SerializeField] private float m_spawnIndicatorDuration = 3f;

    [Header("Starting Room")]
    [SerializeField] private RoomInstance m_startingRoomPrefab;
    [SerializeField] private ChestSettings m_startingChestSettings;

    [Header("Camera")]
    [SerializeField] private CinemachineConfiner2D m_cameraConfiner;
    [Tooltip("Vcam that normally follows the player. Exposed so cinematics can bind Cinemachine shots back to it when they need to cut to the player mid-sequence.")]
    [SerializeField] private CinemachineCamera m_playerVcam;
    [Tooltip("Vcam used to frame the boss during cinematics. Its tracking target is auto-wired to the room's BossEntity on room load and cleared on room unload.")]
    [SerializeField] private CinemachineCamera m_bossVcam;
    [Tooltip("Baseline priority for the boss vcam outside cinematics. Kept lower than the player vcam so it stays idle until a Cinemachine track overrides the blend.")]
    [SerializeField] private int m_bossVcamIdlePriority = -100;

    [Header("Reward Icons")]
    [SerializeField] private List<RewardIconMapping> m_rewardIcons;
    [SerializeField] private Sprite m_bossRoomIcon;
    [SerializeField] private Sprite m_shopRoomIcon;

    private FloorManager m_floorManager;
    private RoomInstance m_currentRoom;
    private RoomEncounter m_currentEncounter;
    private RoomSettings m_currentSettings;
    private EntityHealth m_playerHealth;

    public Transform CurrentRoomTransform => m_currentRoom != null ? m_currentRoom.transform : null;
    public RoomInstance CurrentRoom => m_currentRoom;
    public RoomEncounter CurrentEncounter => m_currentEncounter;
    public CinemachineCamera PlayerVcam => m_playerVcam;
    public CinemachineCamera BossVcam => m_bossVcam;

    public event Action<RoomSettings> OnRoomLoaded;
    public event Action OnRoomCleared;

    private void Awake()
    {
        ServiceLocator.Global.Register(this);

        if (m_player != null)
            m_playerHealth = m_player.GetComponentInChildren<EntityHealth>();

        if (m_bossVcam != null)
            m_bossVcam.Priority.Value = m_bossVcamIdlePriority;
    }

    private void Start()
    {
        ServiceLocator.Global.TryGet(out m_floorManager);

        if (m_floorManager != null)
        {
            m_floorManager.GenerateFloor();
        }

        if (m_startingRoomPrefab != null)
        {
            LoadStartingRoom();
        }
    }

    private void LoadStartingRoom()
    {
        Transform parent = m_roomParent != null ? m_roomParent : transform;
        m_currentRoom = Instantiate(m_startingRoomPrefab, Vector3.zero, Quaternion.identity, parent);
        m_currentRoom.OnRoomCleared += HandleRoomCleared;

        if (m_player != null && m_currentRoom.PlayerSpawnPoint != null)
        {
            m_player.position = m_currentRoom.PlayerSpawnPoint.position;
        }

        bool hasRewardChest = m_currentRoom.RewardChest != null;

        if (hasRewardChest)
        {
            m_currentRoom.RewardChest.Initialize(m_currentRoom, m_startingChestSettings, m_player);
        }

        InitializeBulkheadDoors(hasRewardChest);

        if (hasRewardChest)
        {
            m_currentRoom.OnRewardCollected += UnlockBulkheadDoors;
        }

        UpdateCameraConfiner();

        m_currentRoom.StartRoom();
        m_currentRoom.ClearRoom();
    }

    public void LoadRoom(RoomSettings settings, ChestSettings chestSettings)
    {
        StartCoroutine(TransitionToRoom(settings, chestSettings));
    }

    private IEnumerator TransitionToRoom(RoomSettings settings, ChestSettings chestSettings)
    {
        m_inputReader.DisablePlayerActions();

        if (m_fadeCanvas != null)
        {
            yield return Fade(0f, 1f);
        }

        UnloadCurrentRoom();

        if (m_floorManager != null)
        {
            m_floorManager.AdvanceRoom();
        }

        RoomInstance prefab = settings.GetRandomRoomPrefab();
        if (prefab == null)
        {
            Debug.LogError($"No room prefabs configured for {settings.name}");
            m_inputReader.EnablePlayerActions();
            yield break;
        }

        Transform parent = m_roomParent != null ? m_roomParent : transform;
        m_currentRoom = Instantiate(prefab, Vector3.zero, Quaternion.identity, parent);
        m_currentSettings = settings;

        // Move player to spawn point
        if (m_player != null && m_currentRoom.PlayerSpawnPoint != null)
        {
            m_player.position = m_currentRoom.PlayerSpawnPoint.position;
        }

        bool isCombatRoom = settings.RoomType == RoomType.Combat || settings.RoomType == RoomType.Boss;

        // Initialize reward chest if present
        bool hasRewardChest = false;
        if (m_currentRoom.RewardChest != null && chestSettings != null)
        {
            m_currentRoom.RewardChest.Initialize(m_currentRoom, chestSettings, m_player);
            hasRewardChest = true;
        }

        bool startLocked = isCombatRoom || hasRewardChest;
        InitializeBulkheadDoors(startLocked);

        // Doors unlock when reward is collected, or on room clear if no reward chest
        if (hasRewardChest)
        {
            m_currentRoom.OnRewardCollected += UnlockBulkheadDoors;
        }
        else if (isCombatRoom)
        {
            m_currentRoom.OnRoomCleared += UnlockBulkheadDoors;
        }

        if (isCombatRoom)
        {
            m_currentEncounter = m_currentRoom.gameObject.AddComponent<RoomEncounter>();
            m_currentEncounter.Initialize(m_currentRoom, settings, m_spawnIndicatorPrefab, m_spawnIndicatorDuration);
        }

        m_currentRoom.OnRoomCleared += HandleRoomCleared;

        UpdateCameraConfiner();

        OnRoomLoaded?.Invoke(settings);

        if (m_fadeCanvas != null)
        {
            yield return Fade(1f, 0f);
        }

        // Pre-spawn boss so it's visible during the cinematic
        if (settings is BossRoomSettings bossSettings)
        {
            if (bossSettings.PreSpawnBoss && m_currentEncounter != null)
            {
                m_currentEncounter.PreSpawnBoss();
            }

            RefreshBossCameraTarget();

            if (bossSettings.IntroCinematic != null
                && ServiceLocator.Global.TryGet(out CinematicPlayer cinematicPlayer))
            {
                yield return cinematicPlayer.Play(bossSettings.IntroCinematic);
            }
        }

        m_inputReader.EnablePlayerActions();

        m_currentRoom.StartRoom();

        if (m_currentEncounter != null)
        {
            m_currentEncounter.StartEncounter();
        }
        else
        {
            m_currentRoom.ClearRoom();
        }
    }

    private void InitializeBulkheadDoors(bool startLocked)
    {
        RoomSlot nextSlot = default;
        bool hasNextRoom = false;

        if (m_floorManager != null)
        {
            nextSlot = m_floorManager.GetNextRoomSlot();
            hasNextRoom = nextSlot.Settings != null;
        }

        var usedRewards = new HashSet<RewardType>();

        foreach (BulkheadDoor door in m_currentRoom.BulkheadDoors)
        {
            if (door == null) continue;

            if (!hasNextRoom)
            {
                door.Lock();
                continue;
            }

            // Each door leads to the same next room but offers a different reward
            RewardType reward = m_floorManager.GetRandomRewardTypeExcluding(usedRewards);
            usedRewards.Add(reward);

            ChestSettings chestSettings = m_floorManager.GetChestSettingsForReward(reward);
            Sprite icon = nextSlot.Settings.RoomType switch
            {
                RoomType.Boss => m_bossRoomIcon,
                RoomType.Shop => m_shopRoomIcon,
                _ => GetRewardIcon(reward)
            };
            door.Initialize(nextSlot.Settings, this, chestSettings, icon);

            if (startLocked)
            {
                door.Lock();
            }
            else
            {
                door.Unlock();
            }
        }
    }

    private Sprite GetRewardIcon(RewardType rewardType)
    {
        if (m_rewardIcons == null) return null;

        foreach (var mapping in m_rewardIcons)
        {
            if (mapping.RewardType == rewardType)
                return mapping.Icon;
        }

        return null;
    }

    private void UnlockBulkheadDoors()
    {
        if (m_currentRoom == null) return;

        foreach (BulkheadDoor door in m_currentRoom.BulkheadDoors)
        {
            if (door != null) door.Unlock();
        }
    }

    private void UpdateCameraConfiner()
    {
        if (m_cameraConfiner == null || m_currentRoom == null) return;

        m_cameraConfiner.BoundingShape2D = m_currentRoom.CameraBounds;
        m_cameraConfiner.InvalidateBoundingShapeCache();
    }

    private void RefreshBossCameraTarget()
    {
        if (m_bossVcam == null) return;

        GameObject boss = m_currentEncounter != null ? m_currentEncounter.Boss : null;
        m_bossVcam.Target.TrackingTarget = boss != null ? boss.transform : null;
    }

    public bool IsPlayerInputActive => m_inputReader != null && m_inputReader.IsPlayerActionsEnabled;
    public bool IsGodModeActive => m_playerHealth != null && m_playerHealth.IsGodMode;
    public bool IsCameraConfinerActive => m_cameraConfiner != null && m_cameraConfiner.enabled;

    public void SetPlayerInputActive(bool active)
    {
        if (m_inputReader == null) return;
        if (active)
            m_inputReader.EnablePlayerActions();
        else
            m_inputReader.DisablePlayerActions();
    }

    public void SetPlayerGodMode(bool enabled)
    {
        if (m_playerHealth != null)
            m_playerHealth.IsGodMode = enabled;
    }

    public void SetCameraConfinerActive(bool active)
    {
        if (m_cameraConfiner != null)
            m_cameraConfiner.enabled = active;
    }

    private void HandleRoomCleared()
    {
        OnRoomCleared?.Invoke();
    }

    public void ResetToStartingRoom()
    {
        UnloadCurrentRoom();

        if (m_floorManager != null)
        {
            m_floorManager.Reset();
        }

        m_inputReader.EnablePlayerActions();
        LoadStartingRoom();
    }

    private void UnloadCurrentRoom()
    {
        if (m_currentRoom == null) return;

        m_currentRoom.OnRoomCleared -= HandleRoomCleared;
        m_currentRoom.OnRoomCleared -= UnlockBulkheadDoors;
        m_currentRoom.OnRewardCollected -= UnlockBulkheadDoors;

        if (m_currentEncounter != null)
        {
            m_currentEncounter.CleanUp();
        }

        Destroy(m_currentRoom.gameObject);
        m_currentRoom = null;
        m_currentEncounter = null;
        m_currentSettings = null;

        RefreshBossCameraTarget();
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        m_fadeCanvas.alpha = from;
        m_fadeCanvas.gameObject.SetActive(true);

        while (elapsed < m_fadeDuration)
        {
            elapsed += Time.deltaTime;
            m_fadeCanvas.alpha = Mathf.Lerp(from, to, elapsed / m_fadeDuration);
            yield return null;
        }

        m_fadeCanvas.alpha = to;

        if (to <= 0f)
        {
            m_fadeCanvas.gameObject.SetActive(false);
        }
    }
}

[Serializable]
public struct RewardIconMapping
{
    public RewardType RewardType;
    public Sprite Icon;
}