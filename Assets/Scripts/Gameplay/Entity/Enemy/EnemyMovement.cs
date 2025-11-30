using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyController _enemyController;
    
    [Header("Settings")]
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _attackRange = 1f;
    
    private EnemyTargeting _targeting;

    private void Start()
    {
        _targeting = _enemyController.Targeting;
    }

    public void MoveTowardTarget()
    {
        gameObject.transform.position = Vector2.MoveTowards(gameObject.transform.position, _targeting.CurrentTarget.position, _speed * Time.deltaTime);
    }
    
    public bool IsTargetInRange() => Vector2.Distance(gameObject.transform.position, _targeting.CurrentTarget.position) <= _attackRange;
}
