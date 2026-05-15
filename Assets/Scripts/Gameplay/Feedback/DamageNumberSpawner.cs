using Gameplay.ObjectPool;
using UnityEngine;
using UnityServiceLocator;

public class DamageNumberSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Damage event source. A number spawns on every OnTakeDamageAmount hit.")]
    [SerializeField] private EntityHealth m_health;

    [Tooltip("Pooled DamageNumber prefab spawned on each hit.")]
    [SerializeField] private GameObject m_damageNumberPrefab;

    [Header("Placement")]
    [Tooltip("World-space offset from this transform where the number spawns.")]
    [SerializeField] private Vector3 m_offset = new Vector3(0f, 0.5f, 0f);

    [Tooltip("Random horizontal jitter added to the spawn position, so stacked hits don't overlap.")]
    [SerializeField] private float m_horizontalJitter = 0.2f;

    [Header("Tint Thresholds")]
    [Tooltip("Damage amounts at or above this threshold paint BigHitColor; below, NormalColor.")]
    [SerializeField] private int m_bigHitThreshold = 20;

    [SerializeField] private Color m_normalColor = Color.white;
    [SerializeField] private Color m_bigHitColor = new Color(1f, 0.75f, 0.2f, 1f);

    private ObjectPool m_pool;

    private void Awake()
    {
        ServiceLocator.Global.TryGet(out m_pool);
    }

    private void OnEnable()
    {
        if (m_health != null)
            m_health.OnTakeDamageAmount += HandleDamage;
    }

    private void OnDisable()
    {
        if (m_health != null)
            m_health.OnTakeDamageAmount -= HandleDamage;
    }

    private void HandleDamage(int amount)
    {
        if (amount <= 0 || m_damageNumberPrefab == null || m_pool == null) return;

        GameObject obj = m_pool.GetPooledObject(m_damageNumberPrefab);

        Vector3 position = transform.position + m_offset;
        position.x += Random.Range(-m_horizontalJitter, m_horizontalJitter);
        obj.transform.position = position;

        Color tint = amount >= m_bigHitThreshold ? m_bigHitColor : m_normalColor;
        if (obj.TryGetComponent(out DamageNumber number))
            number.Play(amount, tint);
    }
}
