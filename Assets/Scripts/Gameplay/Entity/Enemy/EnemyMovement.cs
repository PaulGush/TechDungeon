using PlayerObject;
using UnityEngine;
using UnityServiceLocator;

public class EnemyMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyController m_enemyController;
    [SerializeField] private Rigidbody2D m_rigidbody2D;
    [SerializeField] private EnemySettings m_settings;
    public EnemySettings Settings => m_settings;

    private EnemyTargeting m_targeting;

    public bool CanMove;
    public bool Strafe;

    // Context steering directions (pre-computed)
    private Vector2[] m_directions;
    private float[] m_interest;
    private float[] m_danger;

    // Stuck detection
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
        if (!CanMove && !Strafe) return;

        if (m_targeting.CurrentTarget == null)
        {
            if (!ServiceLocator.Global.TryGet(out PlayerMovementController player)) return;
            m_targeting.SetTarget(player.transform);
            if (m_targeting.CurrentTarget == null) return;
        }

        UpdateStuckDetection();

        if (Strafe && !CanMove)
            ApplyStrafe();
        else
            MoveTowardTarget();
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
        float distToTarget = toTarget.magnitude;
        if (distToTarget < 0.01f) return;

        Vector2 desiredDir = toTarget / distToTarget;
        Vector2 steerDir = GetContextSteeringDirection(desiredDir);
        Vector2 separation = GetSeparationForce();

        Vector2 moveDir = (steerDir + separation * m_settings.SeparationWeight).normalized;
        m_rigidbody2D.MovePosition(m_rigidbody2D.position + moveDir * m_settings.Speed * Time.fixedDeltaTime);
    }

    private void ApplyStrafe()
    {
        if (m_settings.StrafeSpeed <= 0f || m_settings.PreferredAttackDistance <= 0f) return;

        Vector2 toTarget = (Vector2)m_targeting.CurrentTarget.position - m_rigidbody2D.position;
        float dist = toTarget.magnitude;
        if (dist < 0.01f) return;

        Vector2 dirToTarget = toTarget / dist;
        Vector2 strafeDir = Vector2.Perpendicular(dirToTarget);

        float distanceDelta = dist - m_settings.PreferredAttackDistance;
        Vector2 distanceCorrection = dirToTarget * Mathf.Clamp(distanceDelta, -1f, 1f) * 0.5f;

        Vector2 desiredDir = (strafeDir + distanceCorrection).normalized;
        Vector2 steerDir = GetContextSteeringDirection(desiredDir);
        Vector2 separation = GetSeparationForce();
        Vector2 moveDir = (steerDir + separation * m_settings.SeparationWeight).normalized;

        m_rigidbody2D.MovePosition(m_rigidbody2D.position + moveDir * m_settings.StrafeSpeed * Time.fixedDeltaTime);
    }

    private Vector2 GetContextSteeringDirection(Vector2 desiredDir)
    {
        int count = m_directions.Length;
        float rayDist = m_settings.ObstacleAvoidanceDistance;
        bool isStuck = m_stuckTimer >= m_settings.StuckTimeBeforeEscape;

        // Build interest map: how well each direction aligns with desired direction
        for (int i = 0; i < count; i++)
        {
            float dot = Vector2.Dot(m_directions[i], desiredDir);
            // When stuck, flatten interest so perpendicular/backward directions become viable
            m_interest[i] = isStuck ? Mathf.Max(dot * 0.3f + 0.7f, 0f) : Mathf.Max(dot, 0f);
            m_danger[i] = 0f;
        }

        // Build danger map: raycast in each direction
        if (m_settings.ObstacleLayerMask != 0)
        {
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = Physics2D.Raycast(m_rigidbody2D.position, m_directions[i], rayDist, m_settings.ObstacleLayerMask);
                if (hit.collider != null)
                {
                    m_danger[i] = 1f - (hit.distance / rayDist);
                }
            }
        }

        // Pick best direction: highest (interest - danger)
        Vector2 chosenDir = desiredDir;
        float bestScore = float.MinValue;

        for (int i = 0; i < count; i++)
        {
            float score = m_interest[i] - m_danger[i];
            if (score > bestScore)
            {
                bestScore = score;
                chosenDir = m_directions[i];
            }
        }

        return chosenDir;
    }

    private Vector2 GetSeparationForce()
    {
        if (m_settings.SeparationRadius <= 0f) return Vector2.zero;

        Vector2 force = Vector2.zero;
        Collider2D[] hits = Physics2D.OverlapCircleAll(m_rigidbody2D.position, m_settings.SeparationRadius, GameConstants.Layers.EnemyLayerMask);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].gameObject == gameObject) continue;
            Vector2 away = m_rigidbody2D.position - (Vector2)hits[i].transform.position;
            float sqrMag = away.sqrMagnitude;
            if (sqrMag > 0f)
                force += away.normalized / Mathf.Sqrt(sqrMag);
        }

        return force;
    }

    public bool IsTargetInRange()
    {
        if (m_targeting.CurrentTarget == null) return false;
        return Vector2.Distance(gameObject.transform.position, m_targeting.CurrentTarget.position) <= m_settings.AttackRange;
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