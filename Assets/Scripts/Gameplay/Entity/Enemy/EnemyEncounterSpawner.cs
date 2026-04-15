using System;
using System.Collections;
using System.Collections.Generic;
using Gameplay.ObjectPool;
using PlayerObject;
using UnityEngine;
using UnityServiceLocator;

public class EnemyEncounterSpawner : MonoBehaviour
{
    [Serializable]
    public class EnemySpawnEntry
    {
        public GameObject EnemyPrefab;
        [Min(1)] public int Weight = 1;
    }

    [Header("Enemies")]
    [SerializeField] private List<EnemySpawnEntry> m_spawnEntries = new();

    [Header("Spawn Settings")]
    [SerializeField, Min(1)] private int m_spawnCount = 3;
    [SerializeField, Min(0f)] private float m_delayBetweenSpawns = 0.3f;

    [Header("Activation")]
    [SerializeField, Min(0.1f)] private float m_triggerRadius = 8f;
    [SerializeField] private bool m_oneShot = true;

    [Header("Spawn Positions")]
    [SerializeField, Min(0.1f)] private float m_spawnRadius = 3f;
    [SerializeField] private List<Transform> m_customSpawnPoints = new();

    public float TriggerRadius => m_triggerRadius;
    public float SpawnRadius => m_spawnRadius;
    public List<Transform> CustomSpawnPoints => m_customSpawnPoints;
    public List<EnemySpawnEntry> SpawnEntries => m_spawnEntries;
    public int SpawnCount => m_spawnCount;
    public float DelayBetweenSpawns => m_delayBetweenSpawns;

    private ObjectPool m_pool;
    private Transform m_player;
    private bool m_hasTriggered;
    private bool m_isSpawning;

    private void Start()
    {
        ServiceLocator.Global.TryGet(out m_pool);

        if (ServiceLocator.Global.TryGet(out PlayerMovementController player))
            m_player = player.transform;
    }

    private void FixedUpdate()
    {
        if (m_hasTriggered && m_oneShot) return;
        if (m_isSpawning) return;
        if (m_player == null) return;

        float dist = Vector2.Distance(transform.position, m_player.position);
        if (dist <= m_triggerRadius)
        {
            m_hasTriggered = true;
            StartCoroutine(SpawnCoroutine());
        }
    }

    private IEnumerator SpawnCoroutine()
    {
        m_isSpawning = true;
        int totalWeight = 0;
        foreach (var entry in m_spawnEntries)
            totalWeight += entry.Weight;

        if (totalWeight <= 0 || m_spawnEntries.Count == 0)
        {
            m_isSpawning = false;
            yield break;
        }

        for (int i = 0; i < m_spawnCount; i++)
        {
            GameObject prefab = PickWeightedRandom(totalWeight);
            if (prefab == null) continue;

            Vector3 spawnPos = GetSpawnPosition(i);

            if (m_pool != null)
            {
                var obj = m_pool.GetPooledObject(prefab);
                obj.transform.position = spawnPos;
            }
            else
            {
                Instantiate(prefab, spawnPos, Quaternion.identity);
            }

            if (m_delayBetweenSpawns > 0 && i < m_spawnCount - 1)
                yield return new WaitForSeconds(m_delayBetweenSpawns);
        }

        m_isSpawning = false;
    }

    private GameObject PickWeightedRandom(int totalWeight)
    {
        int roll = UnityEngine.Random.Range(0, totalWeight);
        int cumulative = 0;

        foreach (var entry in m_spawnEntries)
        {
            cumulative += entry.Weight;
            if (roll < cumulative)
                return entry.EnemyPrefab;
        }

        return m_spawnEntries[^1].EnemyPrefab;
    }

    private Vector3 GetSpawnPosition(int index)
    {
        if (m_customSpawnPoints.Count > 0)
        {
            var point = m_customSpawnPoints[index % m_customSpawnPoints.Count];
            return point != null ? point.position : transform.position;
        }

        Vector2 offset = UnityEngine.Random.insideUnitCircle * m_spawnRadius;
        return transform.position + (Vector3)offset;
    }
}
