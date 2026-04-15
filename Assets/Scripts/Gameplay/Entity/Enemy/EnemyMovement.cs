using System.Collections.Generic;
using PlayerObject;
using UnityEngine;
using UnityServiceLocator;

public class EnemyMovement : MonoBehaviour
{
    // Minimum squared distance the enemy needs to be from its target before attempting to move toward it.
    private const float MinDistanceToTargetSqr = 0.0001f;

    // When the enemy is stuck, interest in each steering direction is remapped to [StuckInterestBase, StuckInterestBase + StuckInterestSlope].
    // This keeps backward/perpendicular directions viable so the enemy can escape dead ends.
    private const float StuckInterestSlope = 0.3f;
    private const float StuckInterestBase = 0.7f;

    // Shared non-allocating buffer for separation queries.
    private static readonly List<Collider2D> s_separationResults = new List<Collider2D>();
    private static ContactFilter2D s_separationFilter;
    private static bool s_separationFilterInitialized;

    [Header("References")]
    [SerializeField] private EnemyController m_enemyController;
    [SerializeField] private Rigidbody2D m_rigidbody2D;
    [SerializeField] private EnemySettings m_settings;
    public EnemySettings Settings => m_settings;

    private EnemyTargeting m_targeting;
    private PlayerMovementController m_cachedPlayer;

    // Minimum squared step before we consider the enemy to actually be moving. Prevents
    // jittery movement from being interpreted as direction changes.
    private const float MovingThresholdSqr = 0.0004f;

    public bool CanMove;
    public bool Strafe;

    // When true, FixedUpdate bails out immediately — the entity is locked in place
    // regardless of CanMove/Strafe. Used by scripted sequences (e.g. the MechSuit's
    // missile-barrage charge-up) that need to override the normal state-machine
    // movement flags without fighting them every frame.
    public bool Frozen;

    private Vector2 m_lastMoveDirection = Vector2.right;
    private bool m_isMoving;

    /// <summary>True if the enemy has taken at least one meaningful step this fixed-update tick.</summary>
    public bool IsMoving => m_isMoving;

    /// <summary>
    /// Unit vector pointing in the direction of the enemy's most recent movement step.
    /// Stable across frames — when the enemy stops, this retains the last travel direction
    /// rather than snapping to a new value. Use this for leg/footstep facing on rigged enemies.
    /// </summary>
    public Vector2 LastMoveDirection => m_lastMoveDirection;

    /// <summary>
    /// Instantly repositions the enemy to a world-space point. Used by scripted
    /// sequences (e.g. the MechSuit's missile-barrage retreat) that need a clean snap
    /// instead of pathing through the level.
    /// </summary>
    public void Teleport(Vector2 worldPosition)
    {
        m_rigidbody2D.position = worldPosition;
    }

    public void SetRuntimeSettings(EnemySettings runtimeSettings)
    {
        m_settings = runtimeSettings;
    }

    // Context steering directions (pre-computed).
    private Vector2[] m_directions;
    private float[] m_interest;
    private float[] m_danger;

    // Stuck detection.
    private Vector2 m_lastPosition;
    private float m_stuckTimer;

    private void Awake()
    {
        m_targeting = m_enemyController.Targeting;
        BuildDirectionTable();
    }

    private void OnEnable()
    {
        m_lastPosition = m_rigidbody2D.position;
        m_stuckTimer = 0f;
    }

    private void BuildDirectionTable()
    {
        int count = m_settings.SteeringRayCount;
        m_directions = new Vector2[count];
        m_interest = new float[count];
        m_danger = new float[count];

        float angleStep = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            m_directions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }

    private void FixedUpdate()
    {
        if (Frozen)
        {
            m_isMoving = false;
            return;
        }

        if (!CanMove && !Strafe)
        {
            m_isMoving = false;
            return;
        }

        // Default to "not moving" each tick; StepInDirection flips it back on when a
        // meaningful step is taken so LastMoveDirection stays stable across stationary frames.
        m_isMoving = false;

        if (!EnsureTarget()) return;

        UpdateStuckDetection();

        if (Strafe && !CanMove)
            ApplyStrafe();
        else
            MoveTowardTarget();
    }

    private bool EnsureTarget()
    {
        if (m_targeting.CurrentTarget != null) return true;

        if (m_cachedPlayer == null && !ServiceLocator.Global.TryGet(out m_cachedPlayer))
            return false;

        if (m_cachedPlayer == null) return false;

        m_targeting.SetTarget(m_cachedPlayer.transform);
        return m_targeting.CurrentTarget != null;
    }

    private void UpdateStuckDetection()
    {
        float distMoved = Vector2.Distance(m_rigidbody2D.position, m_lastPosition);
        if (distMoved < m_settings.StuckThreshold * Time.fixedDeltaTime)
            m_stuckTimer += Time.fixedDeltaTime;
        else
            m_stuckTimer = 0f;

        m_lastPosition = m_rigidbody2D.position;
    }

    private void MoveTowardTarget()
    {
        Vector2 toTarget = (Vector2)m_targeting.CurrentTarget.position - m_rigidbody2D.position;
        float sqrDist = toTarget.sqrMagnitude;
        if (sqrDist < MinDistanceToTargetSqr) return;

        Vector2 desiredDir = toTarget / Mathf.Sqrt(sqrDist);
        StepInDirection(desiredDir, m_settings.Speed);
    }

    private void ApplyStrafe()
    {
        if (m_settings.StrafeSpeed <= 0f || m_settings.PreferredAttackDistance <= 0f) return;

        Vector2 toTarget = (Vector2)m_targeting.CurrentTarget.position - m_rigidbody2D.position;
        float sqrDist = toTarget.sqrMagnitude;
        if (sqrDist < MinDistanceToTargetSqr) return;

        float dist = Mathf.Sqrt(sqrDist);
        Vector2 dirToTarget = toTarget / dist;
        Vector2 strafeDir = Vector2.Perpendicular(dirToTarget);

        float distanceDelta = dist - m_settings.PreferredAttackDistance;
        float clampedDelta = Mathf.Clamp(
            distanceDelta,
            -m_settings.StrafeDistanceCorrectionClamp,
            m_settings.StrafeDistanceCorrectionClamp);
        Vector2 distanceCorrection = dirToTarget * (clampedDelta * m_settings.StrafeDistanceCorrectionStrength);

        Vector2 desiredDir = (strafeDir + distanceCorrection).normalized;
        StepInDirection(desiredDir, m_settings.StrafeSpeed);
    }

    private void StepInDirection(Vector2 desiredDir, float speed)
    {
        Vector2 steerDir = GetContextSteeringDirection(desiredDir);
        Vector2 separation = GetSeparationForce();
        Vector2 moveDir = (steerDir + separation * m_settings.SeparationWeight).normalized;

        Vector2 step = moveDir * (speed * Time.fixedDeltaTime);
        m_rigidbody2D.MovePosition(m_rigidbody2D.position + step);

        if (step.sqrMagnitude >= MovingThresholdSqr)
        {
            m_lastMoveDirection = moveDir;
            m_isMoving = true;
        }
        else
        {
            m_isMoving = false;
        }
    }

    private Vector2 GetContextSteeringDirection(Vector2 desiredDir)
    {
        int count = m_directions.Length;
        float rayDist = m_settings.ObstacleAvoidanceDistance;
        bool isStuck = m_stuckTimer >= m_settings.StuckTimeBeforeEscape;

        // Build interest map: how well each direction aligns with desired direction.
        // When stuck, flatten interest so perpendicular/backward directions become viable escape routes.
        for (int i = 0; i < count; i++)
        {
            float dot = Vector2.Dot(m_directions[i], desiredDir);
            m_interest[i] = isStuck
                ? Mathf.Max(dot * StuckInterestSlope + StuckInterestBase, 0f)
                : Mathf.Max(dot, 0f);
            m_danger[i] = 0f;
        }

        if (m_settings.ObstacleLayerMask != 0)
        {
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = Physics2D.Raycast(m_rigidbody2D.position, m_directions[i], rayDist, m_settings.ObstacleLayerMask);
                if (hit.collider != null)
                    m_danger[i] = 1f - (hit.distance / rayDist);
            }
        }

        // Pick the direction with the highest (interest - danger) score.
        Vector2 chosenDir = desiredDir;
        float bestScore = float.MinValue;

        for (int i = 0; i < count; i++)
        {
            float score = m_interest[i] - m_danger[i];
            if (score <= bestScore) continue;

            bestScore = score;
            chosenDir = m_directions[i];
        }

        return chosenDir;
    }

    private Vector2 GetSeparationForce()
    {
        if (m_settings.SeparationRadius <= 0f) return Vector2.zero;

        if (!s_separationFilterInitialized)
        {
            s_separationFilter = new ContactFilter2D { useTriggers = true };
            s_separationFilter.SetLayerMask(GameConstants.Layers.EnemyLayerMask);
            s_separationFilterInitialized = true;
        }

        int hitCount = Physics2D.OverlapCircle(
            m_rigidbody2D.position,
            m_settings.SeparationRadius,
            s_separationFilter,
            s_separationResults);

        Vector2 force = Vector2.zero;
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D other = s_separationResults[i];
            if (other.gameObject == gameObject) continue;

            Vector2 away = m_rigidbody2D.position - (Vector2)other.transform.position;
            float sqrMag = away.sqrMagnitude;
            if (sqrMag > 0f)
                force += away.normalized / Mathf.Sqrt(sqrMag);
        }

        return force;
    }

    public bool IsTargetInRange()
    {
        if (m_targeting.CurrentTarget == null) return false;
        float attackRangeSqr = m_settings.AttackRange * m_settings.AttackRange;
        return ((Vector2)m_targeting.CurrentTarget.position - (Vector2)transform.position).sqrMagnitude <= attackRangeSqr;
    }

    public bool HasLineOfSight()
    {
        if (m_targeting.CurrentTarget == null) return false;
        if (m_settings.ObstacleLayerMask == 0) return true;

        Vector2 origin = m_rigidbody2D.position;
        Vector2 toTarget = (Vector2)m_targeting.CurrentTarget.position - origin;
        RaycastHit2D hit = Physics2D.Raycast(origin, toTarget.normalized, toTarget.magnitude, m_settings.ObstacleLayerMask);
        return hit.collider == null;
    }
}
