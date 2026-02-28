using System;
using System.Collections;
using System.ComponentModel;
using UnityEngine;

public class Lootable : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private LootableRarity.Rarity _rarity;
    [SerializeField, HideInInspector] private LootableRarity.Rarity _lastRarity;

    [Header("References")]
    [SerializeField] private BounceEffect m_bounceEffect;

    private Vector3 m_aboveChestTargetPosition;
    private Vector3 m_targetPosition;
    private Coroutine m_spawnCoroutine;

    public bool IsSpawning { get; private set; }

    public Action OnSpawnComplete;


    public void StartSpawnSequence(float totalSpawnTime, float spawnTimeInterval, float delay)
    {
        transform.localScale = Vector3.zero;
        IsSpawning = true;
        m_spawnCoroutine = StartCoroutine(SpawnCoroutine(totalSpawnTime, spawnTimeInterval, delay));
    }

    public void SetTargetPosition(Vector3 newValue)
    {
        m_targetPosition = newValue;
    }

    public void SetAboveChestTargetPosition(Vector3 newValue)
    {
        m_aboveChestTargetPosition = newValue;
    }

    private IEnumerator SpawnCoroutine(float totalSpawnTime, float spawnTimeInterval, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        float elapsedTime = 0f;
        WaitForSeconds tick = new WaitForSeconds(spawnTimeInterval);
        bool reachedAboveChestPosition = false;

        while (elapsedTime < totalSpawnTime)
        {
            elapsedTime += spawnTimeInterval;
            float t = Mathf.Clamp01(elapsedTime / totalSpawnTime);
            transform.localScale = Vector3.Slerp(Vector3.zero, Vector3.one, t * 2);

            if (Vector3.Distance(transform.position, m_aboveChestTargetPosition) < 0.01f)
            {
                reachedAboveChestPosition = true;
            }

            transform.position = Vector3.Slerp(transform.position, !reachedAboveChestPosition ? m_aboveChestTargetPosition : m_targetPosition, t);

            yield return tick;
        }
        transform.localScale = Vector3.one;
        transform.position = m_targetPosition;
        FinishSpawn();
    }

    private void FinishSpawn()
    {
        IsSpawning = false;
        m_spawnCoroutine = null;

        if (BounceEffect != null)
        {
            BounceEffect.SetTargets();
            BounceEffect.enabled = true;
        }

        OnSpawnComplete?.Invoke();
    }

    public void CancelSpawn()
    {
        if (!IsSpawning) return;

        if (m_spawnCoroutine != null)
        {
            StopCoroutine(m_spawnCoroutine);
            m_spawnCoroutine = null;
        }

        transform.localScale = Vector3.one;
        transform.position = m_targetPosition;
        IsSpawning = false;
    }

    protected LootableRarity.Rarity m_rarity
    {
        get => _rarity;
        set
        {
            if (!Enum.IsDefined(typeof(LootableRarity.Rarity), value))
                throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(LootableRarity.Rarity));

            _rarity = value;
            _lastRarity = value;
            OnRarityChanged?.Invoke(value);
        }
    }

    public Action<LootableRarity.Rarity> OnRarityChanged;

    public Lootable(Vector3 targetPosition)
    {
        m_targetPosition = targetPosition;
    }

    protected Lootable()
    {

    }

    public void ChangeRarity(LootableRarity.Rarity newValue)
    {
        m_rarity = newValue;
    }

    private void OnValidate()
    {
        if (_rarity != _lastRarity)
        {
            m_rarity = _rarity;
        }
    }

    public BounceEffect BounceEffect
    {
        get
        {
            if (m_bounceEffect == null)
            {
                m_bounceEffect = GetComponent<BounceEffect>();
            }
            return m_bounceEffect;
        }
    }
}
