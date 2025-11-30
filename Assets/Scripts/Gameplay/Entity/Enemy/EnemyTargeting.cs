using UnityEngine;

public class EnemyTargeting : MonoBehaviour
{
    [Header("References")] 
    [SerializeField] private EnemyController _enemyController;
    [SerializeField] private Transform _currentTarget;
    public Transform CurrentTarget => _currentTarget;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.gameObject.CompareTag("Player"))
            return;

        _currentTarget = other.transform;
        _enemyController.StateMachine.ChangeState(_enemyController.StateMachine.SeekState);
    }
    
    public bool IsTargetRightOfTransform() => _currentTarget.position.x > transform.position.x;
}