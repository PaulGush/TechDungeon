using Gameplay.ObjectPool;
using UnityEngine;
using UnityServiceLocator;

public class HealthPickup : Pickup
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
        Interact(other.gameObject);
    }

    public override void Interact(GameObject other)
    {
        if (other.gameObject.layer != GameConstants.Layers.PlayerLayer)
            return;

        if (!other.TryGetComponent(out EntityHealth health))
            return;

        if (health.Heal(m_healAmount))
        {
            m_pool.ReturnGameObject(gameObject);
        }
    }
}
