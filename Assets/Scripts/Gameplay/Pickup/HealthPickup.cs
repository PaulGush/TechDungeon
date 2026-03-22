using Gameplay.ObjectPool;
using UnityEngine;
using UnityServiceLocator;

public class HealthPickup : Lootable
{
    [SerializeField] private int m_healAmount;

    private ObjectPool m_pool;

    private void OnEnable()
    {
        ServiceLocator.Global.TryGet(out ObjectPool pool);
        m_pool = pool;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer != GameConstants.Layers.PlayerLayer)
            return;

        if (!other.TryGetComponent(out EntityHealth health))
            return;

        if (health.Heal(m_healAmount))
        {
            if (m_pool == null || !m_pool.ReturnGameObject(gameObject))
                Destroy(gameObject);
        }
    }
}
