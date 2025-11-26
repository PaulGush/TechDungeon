using System;
using ObjectPool;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D _rigidbody2D;


    [Header("Settings")] 
    [SerializeField] private float _speed = 10f;
    [SerializeField] private int _damage = 10;
    
    //TODO: Extend to scriptable object for settings

    public virtual void Move()
    {
        _rigidbody2D.AddForce( transform.right * _speed);
        
        StartCoroutine(SimplePool.Instance.ReturnAfter(gameObject, 3f));
    }

    private void OnDisable()
    {
        _rigidbody2D.linearVelocity = Vector2.zero;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.TryGetComponent<EntityHealth>(out var entityHealth))
        {
            entityHealth.TakeDamage(_damage);
        }

        SimplePool.Instance.ReturnGameobject(gameObject);
    }
}