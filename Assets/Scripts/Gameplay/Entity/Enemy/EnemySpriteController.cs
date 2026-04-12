using UnityEngine;

public class EnemySpriteController : MonoBehaviour
{
    [SerializeField] private EnemyController m_enemyController;
    [SerializeField] private SpriteRenderer m_spriteRenderer;

    private EnemyTargeting m_targeting;
    private bool m_lastFlipX;
    private bool m_useDirectionalAnimations;

    private static readonly int Rotation = Animator.StringToHash("Rotation");

    private void Awake()
    {
        m_targeting = m_enemyController.Targeting;
    }

    private void Start()
    {
        Animator animator = m_enemyController.AnimationController.GetComponent<Animator>();
        if (animator != null)
        {
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.nameHash == Rotation)
                {
                    m_useDirectionalAnimations = true;
                    break;
                }
            }
        }
    }

    private void Update()
    {
        if (m_useDirectionalAnimations) return;
        if (m_targeting == null || m_targeting.CurrentTarget == null) return;

        bool shouldFlip = !m_targeting.IsTargetRightOfTransform();
        if (shouldFlip != m_lastFlipX)
        {
            m_spriteRenderer.flipX = shouldFlip;
            m_lastFlipX = shouldFlip;
        }
    }
}
