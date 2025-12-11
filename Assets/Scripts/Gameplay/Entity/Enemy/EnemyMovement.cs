using System;
using PlayerObject;
using UnityEngine;
using UnityServiceLocator;

public class EnemyMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyController _enemyController;
    [SerializeField] private Rigidbody2D _rigidbody2D;
    
    [Header("Settings")]
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _attackRange = 1f;
    
    private EnemyTargeting _targeting;

    public bool CanMove;
    
    private void Start()
    {
        _targeting = _enemyController.Targeting;
    }

    private void FixedUpdate()
    {
        if (!CanMove) return;
        MoveTowardTarget();
    }

    private void MoveTowardTarget()
    {
        if (_targeting.CurrentTarget == null)
        {
            _targeting.SetTarget(ServiceLocator.Global.Get<PlayerMovementController>().transform);
        }

        _rigidbody2D.MovePosition(Vector2.MoveTowards(_rigidbody2D.position, _targeting.CurrentTarget.position, _speed * Time.fixedDeltaTime));
    }
    
    public bool IsTargetInRange() => Vector2.Distance(gameObject.transform.position, _targeting.CurrentTarget.position) <= _attackRange;
}