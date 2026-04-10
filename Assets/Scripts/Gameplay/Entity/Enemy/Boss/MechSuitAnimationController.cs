using UnityEngine;

public class MechSuitAnimationController : EnemyAnimationController
{
    private static readonly int Running = Animator.StringToHash("Running");
    private static readonly int LegsRotation = Animator.StringToHash("LegsRotation");
    private static readonly int LegsDead = Animator.StringToHash("LegsDead");

    [Header("Boss - Legs")]
    [SerializeField] private Animator m_legsAnimator;
    [SerializeField] private SpriteRenderer m_legsSpriteRenderer;

    private bool m_torsoHasRunning;
    private bool m_legsHasRotation;
    private bool m_legsHasRunning;
    private bool m_legsHasDead;
    private bool m_legsVisible;

    protected override void OnEnable()
    {
        base.OnEnable();
        CacheMechSuitParameters();
        SetLegsVisible(false);

        if (m_torsoHasRunning) m_animator.SetBool(Running, false);

        if (m_legsAnimator != null)
        {
            if (m_legsHasDead) m_legsAnimator.SetBool(LegsDead, false);
            if (m_legsHasRunning) m_legsAnimator.SetBool(Running, false);
        }
    }

    private void CacheMechSuitParameters()
    {
        m_torsoHasRunning = false;
        foreach (AnimatorControllerParameter param in m_animator.parameters)
        {
            if (param.nameHash == Running) m_torsoHasRunning = true;
        }

        m_legsHasRotation = false;
        m_legsHasRunning = false;
        m_legsHasDead = false;

        if (m_legsAnimator == null) return;

        int rotationHash = LegsRotation;
        int deadHash = LegsDead;

        foreach (AnimatorControllerParameter param in m_legsAnimator.parameters)
        {
            int hash = param.nameHash;
            if (hash == rotationHash) m_legsHasRotation = true;
            else if (hash == Running) m_legsHasRunning = true;
            else if (hash == deadHash) m_legsHasDead = true;
        }
    }

    protected override void Update()
    {
        base.Update();

        // Check if torso is in attack state to toggle legs visibility
        bool isAttacking = m_animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack");
        SetLegsVisible(isAttacking);

        // Drive running state from movement on both torso and legs
        EnemyMovement movement = m_enemyController.Movement;
        bool isMoving = movement != null && (movement.CanMove || movement.Strafe);

        if (m_torsoHasRunning)
            m_animator.SetBool(Running, isMoving);

        if (m_legsAnimator == null) return;

        if (m_legsHasRunning)
            m_legsAnimator.SetBool(Running, isMoving);

        // Sync rotation to legs
        if (m_legsHasRotation && m_targeting.CurrentTarget != null)
        {
            Vector3 diff = (m_targeting.CurrentTarget.position - m_enemyController.transform.position).normalized;
            float angle = Mathf.Repeat(-Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg, 360f);
            m_legsAnimator.SetFloat(LegsRotation, angle);
        }
    }

    protected override void OnDeath()
    {
        SetLegsVisible(false);
        base.OnDeath();

        if (m_legsAnimator != null && m_legsHasDead)
        {
            m_legsAnimator.SetBool(LegsDead, true);
        }
    }

    private void SetLegsVisible(bool visible)
    {
        if (visible == m_legsVisible || m_legsSpriteRenderer == null) return;
        m_legsVisible = visible;
        m_legsSpriteRenderer.enabled = visible;
    }
}
