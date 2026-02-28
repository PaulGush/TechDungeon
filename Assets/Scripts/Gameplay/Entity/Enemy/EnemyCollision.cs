using System;
using UnityEngine;

public class EnemyCollision : MonoBehaviour
{
    [SerializeField] private Collider2D m_collider;
    [SerializeField] private EntityHealth m_health;

    private Action m_onDeathHandler;

    private void Awake()
    {
        m_onDeathHandler = () => m_collider.enabled = false;
    }

    private void OnEnable()
    {
        m_collider.enabled = true;
        m_health.OnDeath += m_onDeathHandler;
    }

    private void OnDisable()
    {
        m_health.OnDeath -= m_onDeathHandler;
    }
}
