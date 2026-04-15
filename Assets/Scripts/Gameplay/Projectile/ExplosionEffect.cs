using System.Collections.Generic;
using Gameplay.ObjectPool;
using UnityEngine;
using UnityServiceLocator;

public class ExplosionEffect : MonoBehaviour
{
    [SerializeField] private CircleCollider2D m_damageCollider;

    [Header("Camera Shake")]
    [SerializeField] private float m_shakeDuration = 0.15f;
    [SerializeField] private float m_shakeAmplitude = 1.5f;
    [SerializeField] private float m_shakeFrequency = 2f;

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

        if (m_damageCollider != null)
            m_damageCollider.radius = m_radius;

        if (m_cameraShake == null)
            ServiceLocator.Global.TryGet(out m_cameraShake);
        if (m_cameraShake != null)
            m_cameraShake.ShakeCam(m_shakeDuration, m_shakeAmplitude, m_shakeFrequency);
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
