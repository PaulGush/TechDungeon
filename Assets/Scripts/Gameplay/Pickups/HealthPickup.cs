using Gameplay.ObjectPool;
using UnityEngine;
using UnityServiceLocator;

public class HealthPickup : Pickup
{
    [SerializeField] private int m_healAmount;
    
    private ObjectPool _pool;
    
    private void OnEnable()
    {
        _pool = ServiceLocator.Global.Get<ObjectPool>();
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
            _pool.ReturnGameObject(gameObject);
        }
    }
}