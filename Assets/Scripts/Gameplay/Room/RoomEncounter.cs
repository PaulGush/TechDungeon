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
    private int m_preSpawnCount;
    private readonly List<MonoBehaviour> m_dormantBehaviours = new List<MonoBehaviour>();

    private GameObject m_spawnIndicatorPrefab;
    private float m_spawnIndicatorDuration;

    /// <summary>
    /// The boss enemy spawned into this room, if any. Set whenever an enemy carrying a
    /// <see cref="BossEntity"/> marker is spawned via <see cref="PreSpawnBoss"/> or
    /// <see cref="SpawnWave"/>. Consumed by RoomManager to wire the boss vcam tracking
    /// target without coupling to a specific prefab.
    /// </summary>
    public GameObject Boss { get; private set; }

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

    public void PreSpawnBoss()
    {
        if (m_waves == null || m_waves.Count == 0 || m_waves[0].EnemyPrefabs.Count == 0)
            return;

        GameObject prefab = m_waves[0].EnemyPrefabs[0];
        if (prefab == null) return;

        Transform spawnPoint = m_roomInstance.GetSpawnPoint(0);
        GameObject boss;

        if (m_pool != null)
        {
            boss = m_pool.GetPooledObject(prefab);
            boss.transform.position = spawnPoint.position;
        }
        else
        {
            boss = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
        }

        // Disable all MonoBehaviours so the boss stays idle during the cinematic.
        // Animator is not a MonoBehaviour, so it keeps running the idle animation.
        foreach (MonoBehaviour mb in boss.GetComponentsInChildren<MonoBehaviour>())
        {
            if (!mb.enabled) continue;

            mb.enabled = false;
            m_dormantBehaviours.Add(mb);
        }

        m_activeEnemies.Add(boss);

        if (boss.GetComponent<BossEntity>() != null)
            Boss = boss;

        EntityHealth health = boss.GetComponent<EntityHealth>();
        if (health != null)
        {
            Action callback = () => OnEnemyDied(boss);
            m_deathCallbacks[boss] = callback;
            health.OnDeath += callback;
        }

        m_preSpawnCount = 1;
    }

    public void StartEncounter()
    {
        if (m_waves == null || m_waves.Count == 0)
        {
            m_roomInstance.ClearRoom();
            return;
        }

        m_currentWaveIndex = 0;

        if (m_preSpawnCount == 0)
        {
            m_activeEnemies.Clear();
            m_deathCallbacks.Clear();
        }

        // Wake up any pre-spawned enemies
        foreach (MonoBehaviour mb in m_dormantBehaviours)
        {
            if (mb != null)
                mb.enabled = true;
        }
        m_dormantBehaviours.Clear();

        StartCoroutine(SpawnWave(m_waves[m_currentWaveIndex]));
    }

    private IEnumerator SpawnWave(EnemyWave wave)
    {
        if (wave.DelayBeforeSpawn > 0f)
        {
            yield return new WaitForSeconds(wave.DelayBeforeSpawn);
        }

        // Skip enemies that were pre-spawned
        int startIndex = 0;
        if (m_currentWaveIndex == 0 && m_preSpawnCount > 0)
        {
            startIndex = m_preSpawnCount;
            m_preSpawnCount = 0;
        }

        // Collect spawn positions for this wave
        var spawnPositions = new List<Vector3>();
        for (int i = startIndex; i < wave.EnemyPrefabs.Count; i++)
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
        for (int i = startIndex; i < wave.EnemyPrefabs.Count; i++)
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

            if (enemy.GetComponent<BossEntity>() != null)
                Boss = enemy;

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

    /// <summary>
    /// Removes every active enemy that isn't the boss from the encounter — unsubscribes
    /// their death callbacks, pulls them out of <see cref="m_activeEnemies"/>, and returns
    /// them to the pool. Used by BossDeathSequence to wipe surviving minions the moment
    /// the boss takes its lethal blow, so the death cutscene plays in a clean room.
    /// </summary>
    public void ClearNonBossEnemies()
    {
        for (int i = m_activeEnemies.Count - 1; i >= 0; i--)
        {
            GameObject enemy = m_activeEnemies[i];
            if (enemy == null || enemy == Boss) continue;

            UnsubscribeEnemy(enemy);
            m_activeEnemies.RemoveAt(i);

            if (m_pool != null)
                m_pool.ReturnGameObject(enemy);
            else
                Destroy(enemy);
        }
    }

    public void RegisterEnemy(GameObject enemy)
    {
        m_activeEnemies.Add(enemy);

        EntityHealth health = enemy.GetComponent<EntityHealth>();
        if (health != null)
        {
            Action callback = () => OnEnemyDied(enemy);
            m_deathCallbacks[enemy] = callback;
            health.OnDeath += callback;
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
        Boss = null;
    }
}
