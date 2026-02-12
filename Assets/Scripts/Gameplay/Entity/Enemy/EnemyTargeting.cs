using UnityEngine;

public class EnemyTargeting : MonoBehaviour
{
    [Header("References")] 
    [SerializeField] private EnemyController m_enemyController;
    [SerializeField] private Transform m_currentTarget;
    public Transform CurrentTarget => m_currentTarget;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.gameObject.CompareTag("Player"))
            return;

        m_currentTarget = other.transform;
        m_enemyController.StateMachine.ChangeState(m_enemyController.StateMachine.SeekState);
    }
    
    public bool IsTargetRightOfTransform() => m_currentTarget.position.x > transform.position.x;

    public void SetTarget(Transform newTarget)
    {
        m_currentTarget = newTarget;
    }
}