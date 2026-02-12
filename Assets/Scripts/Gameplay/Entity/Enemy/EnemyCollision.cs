using UnityEngine;

public class EnemyCollision : MonoBehaviour
{
    [SerializeField] private Collider2D m_collider;
    [SerializeField] private EntityHealth m_health;

    private void OnEnable()
    {
        m_health.OnDeath += () => m_collider.enabled = false;
    }

    private void OnDisable()
    {
        m_health.OnDeath -= () => m_collider.enabled = false;
    }
}