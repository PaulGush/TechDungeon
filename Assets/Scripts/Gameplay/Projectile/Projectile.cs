using Gameplay.ObjectPool;
using UnityEngine;
using UnityServiceLocator;

public class Projectile : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D m_rigidbody2D;
    [SerializeField] private ProjectileSettings m_settings;

    private ObjectPool m_pool;
    private int m_hitsBeforeDeath;
    
    public virtual void Initialize()
    {
        ServiceLocator.Global.Get(out ObjectPool simplePool);
        m_pool = simplePool;
        
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