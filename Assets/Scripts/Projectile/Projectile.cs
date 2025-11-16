using System;
using ObjectPool;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D _rigidbody2D;

    
    [Header("Settings")]
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _damage = 10f;
    
    //TODO: Extend to scriptable object for settings

    public void Move()
    {
        _rigidbody2D.AddForce( transform.right * _speed);
        
        StartCoroutine(SimplePool.Instance.ReturnAfter(gameObject, 3f));
    }

    private void OnDisable()
    {
        _rigidbody2D.linearVelocity = Vector2.zero;
    }
}