using PlayerObject;
using UnityEngine;
using UnityServiceLocator;

public class EnemyMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyController m_enemyController;
    [SerializeField] private Rigidbody2D m_rigidbody2D;
    [SerializeField] private EnemySettings m_settings;

    private EnemyTargeting m_targeting;

    public bool CanMove;

    private void Awake()
    {
        m_targeting = m_enemyController.Targeting;
    }

    private void FixedUpdate()
    {
        if (!CanMove) return;
        MoveTowardTarget();
    }

    private void MoveTowardTarget()
    {
        if (m_targeting.CurrentTarget == null)
        {
            if (!ServiceLocator.Global.TryGet(out PlayerMovementController player)) return;
            m_targeting.SetTarget(player.transform);
        }

        m_rigidbody2D.MovePosition(Vector2.MoveTowards(m_rigidbody2D.position, m_targeting.CurrentTarget.position, m_settings.Speed * Time.fixedDeltaTime));
    }

    public bool IsTargetInRange() => Vector2.Distance(gameObject.transform.position, m_targeting.CurrentTarget.position) <= m_settings.AttackRange;
}
