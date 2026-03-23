using System;
using UnityEngine;

public class EnemyTargeting : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyController m_enemyController;
    [SerializeField] private Transform m_currentTarget;
    public Transform CurrentTarget => m_currentTarget;

    public event Action<Transform> OnTargetChanged;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer != GameConstants.Layers.PlayerLayer)
            return;

        m_currentTarget = other.transform;
        OnTargetChanged?.Invoke(m_currentTarget);
        m_enemyController.StateMachine.ChangeState(m_enemyController.StateMachine.SeekState);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer != GameConstants.Layers.PlayerLayer)
            return;

        if (m_currentTarget == other.transform)
        {
            m_currentTarget = null;
            OnTargetChanged?.Invoke(null);
            m_enemyController.StateMachine.ChangeState(m_enemyController.StateMachine.SeekState);
        }
    }

    public bool IsTargetRightOfTransform() => m_currentTarget != null && m_currentTarget.position.x > transform.position.x;

    public void SetTarget(Transform newTarget)
    {
        m_currentTarget = newTarget;
        OnTargetChanged?.Invoke(m_currentTarget);
    }
}
