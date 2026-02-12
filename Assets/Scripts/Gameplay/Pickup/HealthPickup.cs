using Gameplay.ObjectPool;
using UnityEngine;
using UnityServiceLocator;

public class HealthPickup : Pickup
{
    [SerializeField] private int m_healAmount;
    
    private ObjectPool m_pool;
    
    private void OnEnable()
    {
        m_pool = ServiceLocator.Global.Get<ObjectPool>();
    }
    

    private void OnTriggerEnter2D(Collider2D other)
    {
        Interact(other.gameObject);
    }

    public override void Interact(GameObject other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (!other.TryGetComponent(out EntityHealth health))
            return;

        if (health.Heal(m_healAmount))
        {
            m_pool.ReturnGameObject(gameObject);
        }
    }
}