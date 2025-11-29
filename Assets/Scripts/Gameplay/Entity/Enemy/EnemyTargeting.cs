using UnityEngine;

public class EnemyTargeting : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Collider2D _collider;
    [SerializeField] private Transform _currentTarget;
    public Transform CurrentTarget => _currentTarget;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            _currentTarget = other.transform;
        }
    }
}
