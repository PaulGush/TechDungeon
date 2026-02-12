using System;
using PlayerObject;
using UnityEngine;
using UnityServiceLocator;

public class EnemyMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyController m_enemyController;
    [SerializeField] private Rigidbody2D m_rigidbody2D;
    
    [Header("Settings")]
    [SerializeField] private float m_speed = 10f;
    [SerializeField] private float m_attackRange = 1f;
    
    private EnemyTargeting m_targeting;

    public bool CanMove;
    
    private void Start()
    {
        m_targeting = m_enemyController.Targeting;
    }

    private void FixedUpdate()
    {
        if (!CanMove) return;
        MoveTowardTarget();
    }

    private void MoveTowardTarget()
    {
        if (m_targeting.CurrentTarget == null)
        {
            m_targeting.SetTarget(ServiceLocator.Global.Get<PlayerMovementController>().transform);
        }

        m_rigidbody2D.MovePosition(Vector2.MoveTowards(m_rigidbody2D.position, m_targeting.CurrentTarget.position, m_speed * Time.fixedDeltaTime));
    }
    
    public bool IsTargetInRange() => Vector2.Distance(gameObject.transform.position, m_targeting.CurrentTarget.position) <= m_attackRange;
}