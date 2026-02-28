using Gameplay.ObjectPool;
using UnityEngine;
using UnityServiceLocator;

public class Projectile : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D m_rigidbody2D;
    [SerializeField] private ProjectileSettings m_settings;

    [Header("Collision Filtering")]
    [SerializeField] private LayerMask m_damageLayers;

    private ObjectPool m_pool;
    private int m_hitsBeforeDeath;

    public virtual void Initialize()
    {
        if (m_pool == null)
        {
            ServiceLocator.Global.TryGet(out ObjectPool pool);
            m_pool = pool;
        }

        m_hitsBeforeDeath = m_settings.HitsBeforeDeath;
        m_rigidbody2D.AddForce( transform.right * m_settings.Speed);

        StartCoroutine(m_pool.ReturnAfter(gameObject, m_settings.Lifetime));
    }

    private void OnDisable()
    {
        m_rigidbody2D.linearVelocity = Vector2.zero;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & m_damageLayers) == 0) return;

        if (other.gameObject.TryGetComponent<EntityHealth>(out var entityHealth))
        {
            entityHealth.TakeDamage(m_settings.Damage);
        }

        if (m_hitsBeforeDeath-- <= 0)
        {
            m_pool.ReturnGameObject(gameObject);
        }
    }
}
