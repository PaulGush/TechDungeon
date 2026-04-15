using System.Collections;
using Gameplay.ObjectPool;
using PlayerObject;
using UnityEngine;
using UnityServiceLocator;

/// <summary>
/// Drives the MechSuit's missile-barrage phase end to end. On phase entry the boss
/// teleports to a retreat point and freezes; a self-driven loop then runs a telegraphed
/// volley, snaps to the next retreat point, waits the fire-rate cooldown, and repeats.
/// The boss never walks during this phase — it's a sequence of "snap → volley → snap → volley".
///
/// Per-volley sequence (<see cref="BarrageRoutine"/>):
///   1. Tints the boss sprite red while charging — visually links the warning to the impact indicators.
///   2. Picks N landing sites near the player using rejection sampling for spacing.
///   3. Plays SpawnIndicator-style markers at each landing site.
///   4. Launches a BossMissile from the configured launch point to each site after the telegraph elapses.
///   5. Clears the tint on completion.
/// </summary>
public class MissileBarrage : MonoBehaviour
{
    private const int MaxLandingSiteSampleAttempts = 24;

    [Header("References")]
    [Tooltip("World-space transform from which missiles spawn (e.g., the boss's shoulder cannon).")]
    [SerializeField] private Transform m_missileLaunchPoint;
    [SerializeField] private GameObject m_missilePrefab;
    [SerializeField] private GameObject m_indicatorPrefab;
    [Tooltip("SpriteRenderer tinted while the boss is charging the barrage. Usually the boss torso.")]
    [SerializeField] private SpriteRenderer m_chargeTintRenderer;
    [SerializeField] private BossPhaseManager m_phaseManager;
    [SerializeField] private EnemyMovement m_movement;
    [SerializeField] private EnemyTargeting m_targeting;
    [SerializeField] private EnemyAnimationController m_animationController;
    [SerializeField] private LayerMask m_damageLayers;

    [Header("Charging Visuals")]
    [Tooltip("Color the boss sprite is lerped to while charging the barrage.")]
    [SerializeField] private Color m_chargeTint = new Color(1f, 0.35f, 0.35f, 1f);

    private ObjectPool m_pool;
    private PlayerMovementController m_cachedPlayer;
    private Color m_originalSpriteColor;
    private bool m_capturedSpriteColor;
    private int m_lastRetreatPointIndex = -1;
    private Coroutine m_phaseLoopCoroutine;

    private void Awake()
    {
        ServiceLocator.Global.TryGet(out m_pool);
    }

    private void OnEnable()
    {
        if (m_phaseManager != null)
        {
            m_phaseManager.OnPhaseChanged += HandlePhaseChanged;
            // OnPhaseChanged only fires on index transitions, so if the boss spawns
            // directly into a missile-barrage phase (e.g. pre-damaged revive) we have
            // to enter the phase manually here.
            if (IsMissileBarragePhase(m_phaseManager.CurrentPhase))
                EnterMissileBarragePhase();
        }
    }

    private void HandlePhaseChanged(int phaseIndex)
    {
        if (IsMissileBarragePhase(m_phaseManager.CurrentPhase))
            EnterMissileBarragePhase();
        else
            ExitMissileBarragePhase();
    }

    private static bool IsMissileBarragePhase(BossPhase phase)
        => phase != null && phase.AttackType == BossAttackType.MissileBarrage;

    private Transform ResolveBarrageTarget()
    {
        if (m_targeting != null && m_targeting.CurrentTarget != null)
            return m_targeting.CurrentTarget;

        if (m_cachedPlayer == null)
            ServiceLocator.Global.TryGet(out m_cachedPlayer);

        return m_cachedPlayer != null ? m_cachedPlayer.transform : null;
    }

    private void EnterMissileBarragePhase()
    {
        if (m_phaseLoopCoroutine != null) return;
        if (!ValidateReferences()) return;

        if (m_movement != null)
            m_movement.Frozen = true;

        Vector2? firstRetreat = PickNextRetreatPoint();
        if (firstRetreat.HasValue && m_movement != null)
            m_movement.Teleport(firstRetreat.Value);

        m_phaseLoopCoroutine = StartCoroutine(BarragePhaseLoop());
    }

    private void ExitMissileBarragePhase()
    {
        if (m_phaseLoopCoroutine != null)
        {
            StopCoroutine(m_phaseLoopCoroutine);
            m_phaseLoopCoroutine = null;
        }
        ClearChargeTint();
        if (m_movement != null)
            m_movement.Frozen = false;
    }

    private IEnumerator BarragePhaseLoop()
    {
        while (true)
        {
            // Cooldown between volleys (also acts as the initial delay after phase
            // entry so the opening teleport reads before the first barrage starts).
            float fireRate = m_movement != null && m_movement.Settings != null
                ? m_movement.Settings.FireRate
                : 1f;
            yield return new WaitForSeconds(fireRate);

            BossPhase phase = m_phaseManager.CurrentPhase;
            if (!IsMissileBarragePhase(phase)) yield break;

            // Missile barrage ignores the trigger-based targeting radius — it's a
            // long-range attack, so we resolve the player directly from the global
            // service if EnemyTargeting has nothing (which happens the moment the
            // boss teleports far enough that the player leaves its detection trigger).
            Transform target = ResolveBarrageTarget();
            if (target != null)
                yield return BarrageRoutine(phase, target);

            // Snap to the next retreat point the instant the volley finishes.
            if (m_movement != null)
            {
                Vector2? nextRetreat = PickNextRetreatPoint();
                if (nextRetreat.HasValue)
                    m_movement.Teleport(nextRetreat.Value);
            }
        }
    }

    private bool ValidateReferences()
    {
        bool ok = true;

        if (m_missilePrefab == null)
        {
            Debug.LogWarning($"{nameof(MissileBarrage)}: {nameof(m_missilePrefab)} is not assigned.", this);
            ok = false;
        }
        if (m_missileLaunchPoint == null)
        {
            Debug.LogWarning($"{nameof(MissileBarrage)}: {nameof(m_missileLaunchPoint)} is not assigned.", this);
            ok = false;
        }
        if (m_phaseManager == null)
        {
            Debug.LogWarning($"{nameof(MissileBarrage)}: {nameof(m_phaseManager)} is not assigned — the phase-driven barrage loop can't start.", this);
            ok = false;
        }
        if (m_indicatorPrefab == null)
        {
            Debug.LogWarning($"{nameof(MissileBarrage)}: {nameof(m_indicatorPrefab)} is not assigned — missiles will land with no warning indicator.", this);
            // Not fatal — barrage still fires.
        }
        if (m_movement == null)
        {
            Debug.LogWarning($"{nameof(MissileBarrage)}: {nameof(m_movement)} is not assigned — boss will not teleport or freeze during the phase.", this);
            // Not fatal — barrage still fires.
        }
        if (m_targeting == null)
        {
            Debug.LogWarning($"{nameof(MissileBarrage)}: {nameof(m_targeting)} is not assigned.", this);
            ok = false;
        }
        if (m_animationController == null)
        {
            Debug.LogWarning($"{nameof(MissileBarrage)}: {nameof(m_animationController)} is not assigned — attack pose will not play at launch.", this);
            // Not fatal — barrage still fires.
        }
        if (m_chargeTintRenderer == null)
        {
            Debug.LogWarning($"{nameof(MissileBarrage)}: {nameof(m_chargeTintRenderer)} is not assigned — no visual charge cue.", this);
            // Not fatal — barrage still fires.
        }
        if (m_damageLayers == 0)
        {
            Debug.LogWarning($"{nameof(MissileBarrage)}: {nameof(m_damageLayers)} mask is empty — missiles will deal no damage.", this);
            // Not fatal.
        }

        return ok;
    }

    private IEnumerator BarrageRoutine(BossPhase phase, Transform playerTarget)
    {
        ApplyChargeTint();

        Vector2[] landingSites = PickLandingSites(playerTarget.position, phase);
        SpawnIndicators(landingSites, phase.MissileTelegraphDuration);

        yield return new WaitForSeconds(phase.MissileTelegraphDuration);

        // Fire the attack pose in the same frame as the launch so the animation lines up
        // with the missiles leaving the launch point, rather than playing at the start of
        // the telegraph or after the volley ends.
        if (m_animationController != null)
            m_animationController.OnAttack();

        LaunchMissiles(landingSites, phase);

        // Hold the tint until the slowest missile lands so the visual "charge → release" reads cleanly.
        yield return new WaitForSeconds(phase.MissileTravelDuration);

        ClearChargeTint();
    }

    // Pick a random retreat point, never reusing the previous one. The boss doesn't
    // care about player position during the barrage — it just wants to reposition and
    // volley. Refusing to repeat the last choice prevents the boss from "retreating"
    // in place and keeps the fight visually varied.
    private Vector2? PickNextRetreatPoint()
    {
        if (!ServiceLocator.Global.TryGet(out RoomManager roomManager) || roomManager.CurrentRoom == null)
            return null;

        var retreatPoints = roomManager.CurrentRoom.BossRetreatPoints;
        if (retreatPoints == null || retreatPoints.Count == 0) return null;

        int validCount = 0;
        for (int i = 0; i < retreatPoints.Count; i++)
            if (retreatPoints[i] != null) validCount++;

        if (validCount == 0) return null;

        // If only one valid retreat point exists, reuse it (better than aborting).
        if (validCount == 1)
        {
            for (int i = 0; i < retreatPoints.Count; i++)
            {
                if (retreatPoints[i] == null) continue;
                m_lastRetreatPointIndex = i;
                return retreatPoints[i].position;
            }
        }

        int pickOrdinal = Random.Range(0, validCount - 1);
        int seen = 0;
        for (int i = 0; i < retreatPoints.Count; i++)
        {
            if (retreatPoints[i] == null) continue;
            if (i == m_lastRetreatPointIndex) continue;

            if (seen == pickOrdinal)
            {
                m_lastRetreatPointIndex = i;
                return retreatPoints[i].position;
            }
            seen++;
        }

        return null;
    }

    private Vector2[] PickLandingSites(Vector2 playerPosition, BossPhase phase)
    {
        int count = Mathf.Max(1, phase.MissileCount);
        Vector2[] sites = new Vector2[count];
        float minSeparationSqr = phase.MissileMinSeparation * phase.MissileMinSeparation;

        int placed = 0;
        for (int attempt = 0; attempt < MaxLandingSiteSampleAttempts && placed < count; attempt++)
        {
            Vector2 candidate = playerPosition + Random.insideUnitCircle * phase.MissileSpreadRadius;

            bool tooClose = false;
            for (int i = 0; i < placed; i++)
            {
                if ((sites[i] - candidate).sqrMagnitude < minSeparationSqr)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
                sites[placed++] = candidate;
        }

        // If rejection sampling couldn't fill all slots (e.g., spread radius too tight), pad
        // remaining slots with unconstrained samples so the volley still has its full count.
        for (int i = placed; i < count; i++)
            sites[i] = playerPosition + Random.insideUnitCircle * phase.MissileSpreadRadius;

        return sites;
    }

    private void SpawnIndicators(Vector2[] sites, float duration)
    {
        if (m_indicatorPrefab == null) return;

        for (int i = 0; i < sites.Length; i++)
        {
            GameObject indicator = m_pool != null
                ? m_pool.GetPooledObject(m_indicatorPrefab)
                : Instantiate(m_indicatorPrefab);
            indicator.transform.position = sites[i];

            if (indicator.TryGetComponent(out SpawnIndicator script))
                script.Play(duration);
        }
    }

    private void LaunchMissiles(Vector2[] sites, BossPhase phase)
    {
        Vector2 launchPosition = m_missileLaunchPoint.position;

        for (int i = 0; i < sites.Length; i++)
        {
            GameObject obj = m_pool != null
                ? m_pool.GetPooledObject(m_missilePrefab)
                : Instantiate(m_missilePrefab);

            if (!obj.TryGetComponent(out BossMissile missile)) continue;

            missile.Launch(
                launchPosition,
                sites[i],
                phase.MissileTravelDuration,
                phase.MissileArcHeightRatio,
                phase.MissileDamage,
                phase.MissileExplosionRadius,
                m_damageLayers,
                phase.MissileExplosionEffectPrefab);
        }
    }

    private void ApplyChargeTint()
    {
        if (m_chargeTintRenderer == null) return;

        if (!m_capturedSpriteColor)
        {
            m_originalSpriteColor = m_chargeTintRenderer.color;
            m_capturedSpriteColor = true;
        }
        m_chargeTintRenderer.color = m_chargeTint;
    }

    private void ClearChargeTint()
    {
        if (m_chargeTintRenderer == null || !m_capturedSpriteColor) return;
        m_chargeTintRenderer.color = m_originalSpriteColor;
    }

    private void OnDisable()
    {
        if (m_phaseManager != null)
            m_phaseManager.OnPhaseChanged -= HandlePhaseChanged;

        StopAllCoroutines();
        m_phaseLoopCoroutine = null;
        ClearChargeTint();
        if (m_movement != null)
            m_movement.Frozen = false;
    }
}
