using UnityEngine;

public class MechSuitAnimationController : EnemyAnimationController
{
    private const int BaseLayerIndex = 0;
    private static readonly int AttackTagHash = Animator.StringToHash("Attack");
    private static readonly int Running = Animator.StringToHash("Running");
    private static readonly int LegsDead = Animator.StringToHash("LegsDead");

    [Header("Boss - Legs")]
    [SerializeField] private Animator m_legsAnimator;
    [SerializeField] private SpriteRenderer m_legsSpriteRenderer;

    private bool m_torsoHasRunning;
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

        m_legsHasRunning = false;
        m_legsHasDead = false;

        if (m_legsAnimator == null) return;

        int deadHash = LegsDead;

        foreach (AnimatorControllerParameter param in m_legsAnimator.parameters)
        {
            int hash = param.nameHash;
            if (hash == Running) m_legsHasRunning = true;
            else if (hash == deadHash) m_legsHasDead = true;
        }
    }

    protected override void Update()
    {
        base.Update();

        SetLegsVisible(IsTorsoAttackingOrTransitioningToAttack());

        EnemyMovement movement = m_enemyController.Movement;
        bool isMoving = movement != null && movement.IsMoving;

        if (m_torsoHasRunning)
            m_animator.SetBool(Running, isMoving);

        UpdateLegsFlip();

        if (m_legsAnimator == null) return;

        if (m_legsHasRunning)
            m_legsAnimator.SetBool(Running, isMoving);
    }

    // Mirror the legs sprite horizontally to match the torso aim direction. The legs
    // animator only has Idle/Run states with no directional blend tree, so flipX is the
    // only axis of orientation they have.
    private void UpdateLegsFlip()
    {
        if (m_legsSpriteRenderer == null || m_targeting == null) return;

        Transform target = m_targeting.CurrentTarget;
        if (target == null) return;

        float dx = target.position.x - m_enemyController.transform.position.x;
        if (dx == 0f) return;

        m_legsSpriteRenderer.flipX = dx < 0f;
    }

    /// <summary>
    /// Returns true if the torso is currently playing an Attack-tagged state OR crossfading
    /// into one. Checking both the current and next state prevents a one-frame leg flicker
    /// during transitions, where <see cref="Animator.GetCurrentAnimatorStateInfo"/> reports
    /// the outgoing (non-Attack) state for the duration of the blend.
    /// </summary>
    private bool IsTorsoAttackingOrTransitioningToAttack()
    {
        AnimatorStateInfo current = m_animator.GetCurrentAnimatorStateInfo(BaseLayerIndex);
        if (current.tagHash == AttackTagHash) return true;

        if (m_animator.IsInTransition(BaseLayerIndex))
        {
            AnimatorStateInfo next = m_animator.GetNextAnimatorStateInfo(BaseLayerIndex);
            if (next.tagHash == AttackTagHash) return true;
        }

        return false;
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
