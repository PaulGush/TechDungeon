using System;
using System.Collections;
using System.Collections.Generic;
using Gameplay.ObjectPool;
using UnityEngine;
using UnityServiceLocator;

public class RoomEncounter : MonoBehaviour
{
    private RoomInstance m_roomInstance;
    private List<EnemyWave> m_waves;
    private ObjectPool m_pool;
    private List<GameObject> m_activeEnemies = new List<GameObject>();
    private Dictionary<GameObject, Action> m_deathCallbacks = new Dictionary<GameObject, Action>();
    private int m_currentWaveIndex;

    private GameObject m_spawnIndicatorPrefab;
    private float m_spawnIndicatorDuration;

    public void Initialize(RoomInstance roomInstance, RoomSettings settings,
        GameObject spawnIndicatorPrefab = null, float spawnIndicatorDuration = 0.8f)
    {
        m_roomInstance = roomInstance;
        m_waves = settings.EnemyWaves;
        m_spawnIndicatorPrefab = spawnIndicatorPrefab;
        m_spawnIndicatorDuration = spawnIndicatorDuration;
        ServiceLocator.Global.TryGet(out m_pool);
    }

    public void Initialize(RoomInstance roomInstance, List<EnemyWave> waves,
        GameObject spawnIndicatorPrefab = null, float spawnIndicatorDuration = 0.8f)
    {
        m_roomInstance = roomInstance;
        m_waves = waves;
        m_spawnIndicatorPrefab = spawnIndicatorPrefab;
        m_spawnIndicatorDuration = spawnIndicatorDuration;
        ServiceLocator.Global.TryGet(out m_pool);
    }

    public void StartEncounter()
    {
        if (m_waves == null || m_waves.Count == 0)
        {
            m_roomInstance.ClearRoom();
            return;
        }

        m_currentWaveIndex = 0;
        m_activeEnemies.Clear();
        m_deathCallbacks.Clear();
        StartCoroutine(SpawnWave(m_waves[m_currentWaveIndex]));
    }

    private IEnumerator SpawnWave(EnemyWave wave)
    {
        if (wave.DelayBeforeSpawn > 0f)
        {
            yield return new WaitForSeconds(wave.DelayBeforeSpawn);
        }

        // Collect spawn positions for this wave
        var spawnPositions = new List<Vector3>();
        for (int i = 0; i < wave.EnemyPrefabs.Count; i++)
        {
            if (wave.EnemyPrefabs[i] == null) continue;
            Transform spawnPoint = m_roomInstance.GetSpawnPoint(i);
            spawnPositions.Add(spawnPoint.position);
        }

        // Show spawn indicators
        if (m_spawnIndicatorPrefab != null && spawnPositions.Count > 0)
        {
            foreach (Vector3 pos in spawnPositions)
            {
                GameObject indicator = m_pool != null
                    ? m_pool.GetPooledObject(m_spawnIndicatorPrefab)
                    : Instantiate(m_spawnIndicatorPrefab);
                indicator.transform.position = pos;
                SpawnIndicator script = indicator.GetComponent<SpawnIndicator>();
                script?.Play(m_spawnIndicatorDuration);
            }

            yield return new WaitForSeconds(m_spawnIndicatorDuration);
        }

        // Spawn enemies
        for (int i = 0; i < wave.EnemyPrefabs.Count; i++)
        {
            GameObject prefab = wave.EnemyPrefabs[i];
            if (prefab == null) continue;

            Transform spawnPoint = m_roomInstance.GetSpawnPoint(i);
            GameObject enemy;

            if (m_pool != null)
            {
                enemy = m_pool.GetPooledObject(prefab);
                enemy.transform.position = spawnPoint.position;
            }
            else
            {
                enemy = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
            }

            m_activeEnemies.Add(enemy);

            EntityHealth health = enemy.GetComponent<EntityHealth>();
            if (health != null)
            {
                GameObject captured = enemy;
                Action callback = () => OnEnemyDied(captured);
                m_deathCallbacks[enemy] = callback;
                health.OnDeath += callback;
            }
        }
    }

    private void OnEnemyDied(GameObject enemy)
    {
        UnsubscribeEnemy(enemy);
        m_activeEnemies.Remove(enemy);

        EnemyController ec = enemy.GetComponent<EnemyController>();
        if (ec != null && ServiceLocator.Global.TryGet(out CreditManager creditManager))
        {
            creditManager.AddCredits(ec.CreditValue);
        }

        if (m_activeEnemies.Count > 0) return;

        m_currentWaveIndex++;
        if (m_currentWaveIndex < m_waves.Count)
        {
            StartCoroutine(SpawnWave(m_waves[m_currentWaveIndex]));
        }
        else
        {
            m_roomInstance.ClearRoom();
        }
    }

    private void UnsubscribeEnemy(GameObject enemy)
    {
        if (!m_deathCallbacks.TryGetValue(enemy, out Action callback)) return;

        EntityHealth health = enemy.GetComponent<EntityHealth>();
        if (health != null)
        {
            health.OnDeath -= callback;
        }

        m_deathCallbacks.Remove(enemy);
    }

    public void KillAllEnemies()
    {
        StopAllCoroutines();

        var enemiesToKill = new List<GameObject>(m_activeEnemies);
        foreach (var enemy in enemiesToKill)
        {
            if (enemy == null) continue;

            EntityHealth health = enemy.GetComponent<EntityHealth>();
            if (health != null && !health.IsDead)
            {
                health.TakeDamage(health.CurrentHealth + health.Armor);
            }
        }
    }

    public void CleanUp()
    {
        StopAllCoroutines();

        foreach (var enemy in m_activeEnemies)
        {
            if (enemy == null) continue;

            UnsubscribeEnemy(enemy);

            if (m_pool != null)
            {
                m_pool.ReturnGameObject(enemy);
            }
            else
            {
                Destroy(enemy);
            }
        }

        m_activeEnemies.Clear();
        m_deathCallbacks.Clear();
    }
}
