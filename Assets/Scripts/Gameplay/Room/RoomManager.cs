using System;
using System.Collections;
using System.Collections.Generic;
using Input;
using UnityEngine;
using UnityServiceLocator;

public class RoomManager : MonoBehaviour
{
    [Header("Room Pool")]
    [SerializeField] private List<RoomSettings> m_roomPool;

    [Header("Player")]
    [SerializeField] private Transform m_player;
    [SerializeField] private InputReader m_inputReader;

    [Header("Transition")]
    [SerializeField] private CanvasGroup m_fadeCanvas;
    [SerializeField] private float m_fadeDuration = 0.5f;

    [Header("Room Spawn")]
    [SerializeField] private Transform m_roomParent;

    [Header("Starting Room")]
    [SerializeField] private RoomInstance m_startingRoom;

    private RoomInstance m_currentRoom;
    private RoomEncounter m_currentEncounter;
    private RoomSettings m_currentSettings;

    public Transform CurrentRoomTransform => m_currentRoom != null ? m_currentRoom.transform : null;

    public event Action<RoomSettings> OnRoomLoaded;
    public event Action OnRoomCleared;

    private void Awake()
    {
        ServiceLocator.Global.Register(this);
    }

    private void Start()
    {
        if (m_startingRoom != null)
        {
            SetUpStartingRoom();
        }
    }

    private void SetUpStartingRoom()
    {
        m_currentRoom = m_startingRoom;
        m_currentRoom.OnRoomCleared += HandleRoomCleared;

        InitializeBulkheadDoors(false);

        m_currentRoom.StartRoom();
        m_currentRoom.ClearRoom();
    }

    public void LoadRoom(RoomSettings settings)
    {
        StartCoroutine(TransitionToRoom(settings));
    }

    private IEnumerator TransitionToRoom(RoomSettings settings)
    {
        m_inputReader.DisablePlayerActions();

        if (m_fadeCanvas != null)
        {
            yield return Fade(0f, 1f);
        }

        UnloadCurrentRoom();

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

        InitializeBulkheadDoors(isCombatRoom);

        if (isCombatRoom)
        {
            m_currentEncounter = m_currentRoom.gameObject.AddComponent<RoomEncounter>();
            m_currentEncounter.Initialize(m_currentRoom, settings);
        }

        m_currentRoom.OnRoomCleared += HandleRoomCleared;

        OnRoomLoaded?.Invoke(settings);

        if (m_fadeCanvas != null)
        {
            yield return Fade(1f, 0f);
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
        foreach (BulkheadDoor door in m_currentRoom.BulkheadDoors)
        {
            if (door == null) continue;

            RoomSettings nextRoom = GetRandomRoomFromPool();
            door.Initialize(nextRoom, this);

            if (startLocked)
            {
                door.Lock();
            }
            else
            {
                door.Unlock();
            }
        }

        if (startLocked)
        {
            m_currentRoom.OnRoomCleared += UnlockBulkheadDoors;
        }
    }

    private void UnlockBulkheadDoors()
    {
        if (m_currentRoom == null) return;

        foreach (BulkheadDoor door in m_currentRoom.BulkheadDoors)
        {
            if (door != null) door.Unlock();
        }
    }

    private void HandleRoomCleared()
    {
        OnRoomCleared?.Invoke();
    }

    private RoomSettings GetRandomRoomFromPool()
    {
        if (m_roomPool == null || m_roomPool.Count == 0) return null;
        return m_roomPool[UnityEngine.Random.Range(0, m_roomPool.Count)];
    }

    private void UnloadCurrentRoom()
    {
        if (m_currentRoom == null) return;

        m_currentRoom.OnRoomCleared -= HandleRoomCleared;
        m_currentRoom.OnRoomCleared -= UnlockBulkheadDoors;

        if (m_currentEncounter != null)
        {
            m_currentEncounter.CleanUp();
        }

        Destroy(m_currentRoom.gameObject);
        m_currentRoom = null;
        m_currentEncounter = null;
        m_currentSettings = null;
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
