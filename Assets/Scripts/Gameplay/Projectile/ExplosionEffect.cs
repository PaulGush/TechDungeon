using System.Collections.Generic;
using Gameplay.ObjectPool;
using UnityEngine;
using UnityServiceLocator;

public class ExplosionEffect : MonoBehaviour
{
    [SerializeField] private CircleCollider2D m_damageCollider;

    [Header("Camera Shake")]
    [Tooltip("Impulse amplitude applied to the camera shake service on detonation. Sits in the same scale as CameraShake — 0.3 is a noticeable punch, 1.0 is a large boss-grade shake.")]
    [SerializeField] private float m_shakeAmplitude = 0.3f;

    [Header("Visual Scaling")]
    [Tooltip("Multiplier on the explosion radius used to scale the prefab's transform on spawn, so the sprite/light visibly grow with the damage area. The damage collider is compensated so world-space damage still matches the radius. Default is 2 so the authored 1-unit-wide sprite visually fills a diameter of 2R — matching the targeting indicator.")]
    [SerializeField] private float m_visualScalePerRadiusUnit = 2f;

    private ObjectPool m_pool;
    private CameraShake m_cameraShake;
    private float m_radius;
    private int m_baseDamage;
    private LayerMask m_damageLayers;
    private readonly HashSet<EntityHealth> m_damaged = new();

    private void Start()
    {
        ServiceLocator.Global.TryGet(out ObjectPool pool);
        m_pool = pool;
    }

    public void Initialize(float radius, int damage, LayerMask damageLayers)
    {
        m_radius = Mathf.Max(radius, 0.0001f);
        m_baseDamage = damage;
        m_damageLayers = damageLayers;
        m_damaged.Clear();

        float visualScale = Mathf.Max(m_radius * m_visualScalePerRadiusUnit, 0.0001f);
        transform.localScale = Vector3.one * visualScale;

        if (m_damageCollider != null)
        {
            // Transform scale multiplies the collider radius — compensate so the
            // world-space damage radius stays m_radius regardless of visual scale.
            m_damageCollider.radius = m_radius / visualScale;
        }

        if (m_cameraShake == null)
            ServiceLocator.Global.TryGet(out m_cameraShake);
        if (m_cameraShake != null)
            m_cameraShake.Shake(m_shakeAmplitude);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (m_baseDamage <= 0) return;
        if ((m_damageLayers.value & (1 << other.gameObject.layer)) == 0) return;
        if (!other.TryGetComponent(out EntityHealth health)) return;
        if (!m_damaged.Add(health)) return;

        float distance = Vector2.Distance(transform.position, other.transform.position);
        float falloff = 1f - Mathf.Clamp01(distance / m_radius);
        int damage = Mathf.RoundToInt(m_baseDamage * falloff);
        if (damage > 0)
            health.TakeDamage(damage);
    }

    public void ReturnToPool()
    {
        m_pool.ReturnGameObject(gameObject);
    }
}
