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
    [Tooltip("SpriteRenderers tinted while the boss is charging the barrage (e.g., torso + legs). Original colors are cached on first tint and restored on clear.")]
    [SerializeField] private SpriteRenderer[] m_chargeTintRenderers;
    [SerializeField] private BossPhaseManager m_phaseManager;
    [SerializeField] private EnemyMovement m_movement;
    [SerializeField] private EnemyTargeting m_targeting;
    [SerializeField] private EnemyAnimationController m_animationController;
    [SerializeField] private LayerMask m_damageLayers;

    [Header("Charging Visuals")]
    [Tooltip("Color the boss sprite is lerped to while charging the barrage.")]
    [SerializeField] private Color m_chargeTint = new Color(1f, 0.35f, 0.35f, 1f);

    [Header("Teleport VFX")]
    [Tooltip("Pool-friendly puff prefab spawned at the source and destination of each teleport. Leave empty to skip the puff.")]
    [SerializeField] private GameObject m_teleportPuffPrefab;

    [Tooltip("HitFlashes driven on teleport (e.g., torso + legs). Leave empty to skip the flash.")]
    [SerializeField] private HitFlash[] m_teleportHitFlashes;

    [Tooltip("Color fed to the teleport HitFlashes. Keep it distinct from the damage flash so players can read the reposition.")]
    [SerializeField] private Color m_teleportFlashColor = new Color(0.6f, 0.85f, 1f, 1f);

    [Tooltip("Seconds the boss charges in place before the snap. Zero disables the wind-up.")]
    [SerializeField] private float m_teleportWindUpDuration = 0.2f;

    [Tooltip("Renderers that fade down during wind-up and fade back in on arrival. Typically the same set as the charge tint / HitFlash renderers.")]
    [SerializeField] private SpriteRenderer[] m_teleportFadeRenderers;

    [Tooltip("Transform scaled during the teleport. Typically the boss root. Leave empty to disable the scale tween.")]
    [SerializeField] private Transform m_teleportScaleTarget;

    [Tooltip("Scale multiplier reached at the end of wind-up. 0 fully collapses; 0.1-0.3 reads as 'compressing into the puff'.")]
    [Range(0f, 1f)]
    [SerializeField] private float m_teleportVanishScale = 0.2f;

    [Tooltip("Alpha multiplier reached at the end of wind-up. 0 fully hides the boss during the snap.")]
    [Range(0f, 1f)]
    [SerializeField] private float m_teleportVanishAlpha = 0f;

    [Tooltip("Scale multiplier the boss first appears at on arrival. Above 1 gives a pop-in overshoot; below 1 grows out of nothing.")]
    [SerializeField] private float m_teleportArrivalScale = 1.3f;

    [Tooltip("Seconds the arrival tween takes to return to the original scale and alpha after the snap.")]
    [SerializeField] private float m_teleportArrivalDuration = 0.15f;

    [Header("Teleport Hit-Stop")]
    [Tooltip("Seconds Time.timeScale is pinned near zero at the moment of the snap, to sell the reposition. Zero disables the hit-stop.")]
    [SerializeField] private float m_teleportHitStopDuration = 0.06f;

    [Tooltip("Time.timeScale value held during the teleport hit-stop. Values near zero (not exactly zero) work best.")]
    [SerializeField] private float m_teleportHitStopTimeScale = 0.02f;

    private ObjectPool m_pool;
    private PlayerMovementController m_cachedPlayer;
    private HitStopService m_hitStopService;
    private Color[] m_originalTintColors;
    private bool m_capturedTintColors;
    private Color[] m_teleportBaseColors;
    private bool m_capturedTeleportColors;
    private Vector3 m_teleportBaseScale;
    private bool m_capturedTeleportScale;
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

        m_phaseLoopCoroutine = StartCoroutine(BarragePhaseLoop(PickNextRetreatPoint()));
    }

    private void ExitMissileBarragePhase()
    {
        if (m_phaseLoopCoroutine != null)
        {
            StopCoroutine(m_phaseLoopCoroutine);
            m_phaseLoopCoroutine = null;
        }
        ClearChargeTint();
        RestoreTeleportVisuals();
        if (m_movement != null)
            m_movement.Frozen = false;
    }

    // Wraps EnemyMovement.Teleport with a wind-up (flash + source puff + scale/fade
    // collapse), the snap (with hit-stop), and arrival (destination puff + flash +
    // scale/fade pop-in), so the reposition reads as an intentional beat rather
    // than a silent snap.
    private IEnumerator DoTeleport(Vector2 destination)
    {
        if (m_movement == null) yield break;

        Vector2 source = transform.position;
        CaptureTeleportBaselines();

        FlashTeleport();
        SpawnTeleportPuff(source);

        // Wind-up: collapse the boss's scale + alpha so it visibly "compresses into the puff".
        if (m_teleportWindUpDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < m_teleportWindUpDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / m_teleportWindUpDuration);
                // Ease-in so the collapse accelerates toward the snap.
                float eased = t * t;
                float scaleMult = Mathf.Lerp(1f, m_teleportVanishScale, eased);
                float alphaMult = Mathf.Lerp(1f, m_teleportVanishAlpha, eased);
                ApplyTeleportVisuals(scaleMult, alphaMult);
                yield return null;
            }
            ApplyTeleportVisuals(m_teleportVanishScale, m_teleportVanishAlpha);
        }

        m_movement.Teleport(destination);
        SpawnTeleportPuff(destination);
        FlashTeleport();

        if (m_teleportHitStopDuration > 0f)
        {
            if (m_hitStopService == null)
                ServiceLocator.Global.TryGet(out m_hitStopService);
            if (m_hitStopService != null)
                m_hitStopService.Freeze(m_teleportHitStopDuration, m_teleportHitStopTimeScale);
        }

        // Arrival: pop back to full scale/alpha (optionally overshoot) from the vanish state.
        if (m_teleportArrivalDuration > 0f)
        {
            // Prime the starting state so the first frame renders at arrival-start values
            // even if the wind-up was skipped.
            ApplyTeleportVisuals(m_teleportArrivalScale, m_teleportVanishAlpha);

            float elapsed = 0f;
            while (elapsed < m_teleportArrivalDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / m_teleportArrivalDuration);
                // Ease-out so the overshoot settles smoothly into rest.
                float eased = 1f - (1f - t) * (1f - t);
                float scaleMult = Mathf.Lerp(m_teleportArrivalScale, 1f, eased);
                float alphaMult = Mathf.Lerp(m_teleportVanishAlpha, 1f, eased);
                ApplyTeleportVisuals(scaleMult, alphaMult);
                yield return null;
            }
        }

        ApplyTeleportVisuals(1f, 1f);
    }

    private void FlashTeleport()
    {
        if (m_teleportHitFlashes == null) return;
        for (int i = 0; i < m_teleportHitFlashes.Length; i++)
        {
            if (m_teleportHitFlashes[i] != null)
                m_teleportHitFlashes[i].Flash(m_teleportFlashColor);
        }
    }

    private void CaptureTeleportBaselines()
    {
        if (!m_capturedTeleportColors && m_teleportFadeRenderers != null && m_teleportFadeRenderers.Length > 0)
        {
            m_teleportBaseColors = new Color[m_teleportFadeRenderers.Length];
            for (int i = 0; i < m_teleportFadeRenderers.Length; i++)
            {
                if (m_teleportFadeRenderers[i] != null)
                    m_teleportBaseColors[i] = m_teleportFadeRenderers[i].color;
            }
            m_capturedTeleportColors = true;
        }

        if (!m_capturedTeleportScale && m_teleportScaleTarget != null)
        {
            m_teleportBaseScale = m_teleportScaleTarget.localScale;
            m_capturedTeleportScale = true;
        }
    }

    private void ApplyTeleportVisuals(float scaleMultiplier, float alphaMultiplier)
    {
        if (m_capturedTeleportScale && m_teleportScaleTarget != null)
            m_teleportScaleTarget.localScale = m_teleportBaseScale * scaleMultiplier;

        if (m_capturedTeleportColors && m_teleportFadeRenderers != null)
        {
            for (int i = 0; i < m_teleportFadeRenderers.Length; i++)
            {
                if (m_teleportFadeRenderers[i] == null) continue;
                if (i >= m_teleportBaseColors.Length) continue;
                Color baseColor = m_teleportBaseColors[i];
                m_teleportFadeRenderers[i].color = new Color(
                    baseColor.r,
                    baseColor.g,
                    baseColor.b,
                    baseColor.a * alphaMultiplier);
            }
        }
    }

    private void RestoreTeleportVisuals()
    {
        if (!m_capturedTeleportColors && !m_capturedTeleportScale) return;
        ApplyTeleportVisuals(1f, 1f);
    }

    private void SpawnTeleportPuff(Vector2 position)
    {
        if (m_teleportPuffPrefab == null) return;

        GameObject puff = m_pool != null
            ? m_pool.GetPooledObject(m_teleportPuffPrefab)
            : Instantiate(m_teleportPuffPrefab);
        puff.transform.position = position;
    }

    private IEnumerator BarragePhaseLoop(Vector2? initialRetreat)
    {
        // Initial teleport runs inside the loop so its wind-up can yield.
        if (initialRetreat.HasValue)
            yield return DoTeleport(initialRetreat.Value);

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

            Vector2? nextRetreat = PickNextRetreatPoint();
            if (nextRetreat.HasValue)
                yield return DoTeleport(nextRetreat.Value);
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
        if (m_chargeTintRenderers == null || m_chargeTintRenderers.Length == 0)
        {
            Debug.LogWarning($"{nameof(MissileBarrage)}: {nameof(m_chargeTintRenderers)} is empty — no visual charge cue.", this);
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
        SpawnIndicators(landingSites, phase.MissileTelegraphDuration, phase.MissileExplosionRadius);

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

    private void SpawnIndicators(Vector2[] sites, float duration, float explosionRadius)
    {
        if (m_indicatorPrefab == null) return;

        // SpawnIndicator uses a 1-unit-wide circle sprite (radius 0.5 at scale 1),
        // so localScale must be 2× the explosion radius to match the blast footprint.
        float targetScale = Mathf.Max(explosionRadius, 0f) * 2f;

        for (int i = 0; i < sites.Length; i++)
        {
            GameObject indicator = m_pool != null
                ? m_pool.GetPooledObject(m_indicatorPrefab)
                : Instantiate(m_indicatorPrefab);
            indicator.transform.position = sites[i];

            if (indicator.TryGetComponent(out SpawnIndicator script))
                script.Play(duration, targetScale);
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
        if (m_chargeTintRenderers == null || m_chargeTintRenderers.Length == 0) return;

        if (!m_capturedTintColors)
        {
            m_originalTintColors = new Color[m_chargeTintRenderers.Length];
            for (int i = 0; i < m_chargeTintRenderers.Length; i++)
            {
                if (m_chargeTintRenderers[i] != null)
                    m_originalTintColors[i] = m_chargeTintRenderers[i].color;
            }
            m_capturedTintColors = true;
        }

        for (int i = 0; i < m_chargeTintRenderers.Length; i++)
        {
            if (m_chargeTintRenderers[i] != null)
                m_chargeTintRenderers[i].color = m_chargeTint;
        }
    }

    private void ClearChargeTint()
    {
        if (m_chargeTintRenderers == null || !m_capturedTintColors) return;

        for (int i = 0; i < m_chargeTintRenderers.Length; i++)
        {
            if (m_chargeTintRenderers[i] != null && i < m_originalTintColors.Length)
                m_chargeTintRenderers[i].color = m_originalTintColors[i];
        }
    }

    private void OnDisable()
    {
        if (m_phaseManager != null)
            m_phaseManager.OnPhaseChanged -= HandlePhaseChanged;

        StopAllCoroutines();
        m_phaseLoopCoroutine = null;
        ClearChargeTint();
        RestoreTeleportVisuals();
        if (m_movement != null)
            m_movement.Frozen = false;
    }
}
