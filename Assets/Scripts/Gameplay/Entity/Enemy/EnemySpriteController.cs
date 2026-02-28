using UnityEngine;

public class EnemySpriteController : MonoBehaviour
{
    [SerializeField] private EnemyController m_enemyController;
    [SerializeField] private SpriteRenderer m_spriteRenderer;

    private EnemyTargeting m_targeting;
    private bool m_lastFlipX;

    private void Awake()
    {
        m_targeting = m_enemyController.Targeting;
    }

    private void Update()
    {
        if (m_targeting == null || m_targeting.CurrentTarget == null) return;

        bool shouldFlip = !m_targeting.IsTargetRightOfTransform();
        if (shouldFlip != m_lastFlipX)
        {
            m_spriteRenderer.flipX = shouldFlip;
            m_lastFlipX = shouldFlip;
        }
    }
}
