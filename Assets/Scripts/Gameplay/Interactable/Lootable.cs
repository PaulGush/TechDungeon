using System;
using System.Collections;
using System.ComponentModel;
using UnityEngine;

public class Lootable : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private LootableRarity.Rarity _rarity;
    [SerializeField, HideInInspector] private LootableRarity.Rarity _lastRarity;

    private Vector3 m_aboveChestTargetPosition;
    private Vector3 m_targetPosition;

    public void StartSpawnSequence(float totalSpawnTime, float spawnTimeInterval, float delay)
    {
        transform.localScale = Vector3.zero;
        StartCoroutine(SpawnCoroutine(totalSpawnTime, spawnTimeInterval, delay));
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

            if (transform.position == m_aboveChestTargetPosition)
            {
                reachedAboveChestPosition = true;
            }
            
            transform.position = Vector3.Slerp(transform.position, !reachedAboveChestPosition ? m_aboveChestTargetPosition : m_targetPosition, t);
            
            yield return tick;
        }
        transform.localScale = Vector3.one;
        transform.position = m_targetPosition;
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

    public Lootable()
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
}