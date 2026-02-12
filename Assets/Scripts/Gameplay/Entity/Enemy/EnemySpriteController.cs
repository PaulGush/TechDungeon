using UnityEngine;

public class EnemySpriteController : MonoBehaviour
{
    [SerializeField] private EnemyController m_enemyController;
    [SerializeField] private SpriteRenderer m_spriteRenderer;
    
    private EnemyTargeting m_targeting;

    private void Awake()
    {
        m_targeting = m_enemyController.Targeting;
    }

    private void Update()
    {
        if (m_targeting == null || m_targeting.CurrentTarget == null) return;
        m_spriteRenderer.flipX = !m_targeting.IsTargetRightOfTransform();
    }
}