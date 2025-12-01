using ObjectPool;
using UnityEngine;
using UnityServiceLocator;

public class Projectile : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D _rigidbody2D;
    [SerializeField] private ProjectileSettings _settings;

    private SimplePool _pool;
    private int _hitsBeforeDeath;
    
    public virtual void Initialize()
    {
        ServiceLocator.Global.Get(out SimplePool simplePool);
        _pool = simplePool;
        
        _hitsBeforeDeath = _settings.HitsBeforeDeath;
        _rigidbody2D.AddForce( transform.right * _settings.Speed);
        
        StartCoroutine(_pool.ReturnAfter(gameObject, _settings.Lifetime));
    }

    private void OnDisable()
    {
        _rigidbody2D.linearVelocity = Vector2.zero;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.TryGetComponent<EntityHealth>(out var entityHealth))
        {
            entityHealth.TakeDamage(_settings.Damage);
        }

        if (_hitsBeforeDeath-- <= 0)
        {
            _pool.ReturnGameobject(gameObject);
        }
    }
}