using UnityEngine;

public class EnemyCollision : MonoBehaviour
{
    [SerializeField] private Collider2D _collider;
    [SerializeField] private EntityHealth _health;

    private void OnEnable()
    {
        _health.OnDeath += () => _collider.enabled = false;
    }

    private void OnDisable()
    {
        _health.OnDeath -= () => _collider.enabled = false;
    }
}
