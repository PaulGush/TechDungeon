using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Timeline;
using UnityServiceLocator;

/// <summary>
/// Drives the MechSuit's death cutscene end to end. Hooks <see cref="EntityHealth.DeathInterceptor"/>
/// so the killing blow is absorbed at 1 HP, halts the boss's enemy logic, gates player
/// input off, switches to the boss vcam, ramps Time.timeScale to 0, plays a cinematic via
/// <see cref="CinematicPlayer"/> (which runs unscaled), then plays the boss's existing
/// death animation (which contains the explosion) before formally killing the boss so
/// RoomEncounter sees OnDeath fire and clears the room.
/// The default death animator subscription must be suppressed via
/// <c>EnemyAnimationController.m_suppressDefaultDeathAnimation</c> on the boss prefab so
/// this sequence can stage everything in order.
/// </summary>
public class BossDeathSequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EntityHealth m_health;
    [SerializeField] private EnemyController m_enemyController;
    [SerializeField] private EnemyAnimationController m_animationController;
    [SerializeField] private TimelineAsset m_deathCinematic;

    [Header("Time Halt")]
    [Tooltip("How long to ramp Time.timeScale from current down to the halt target. Keep short — should feel near-instant to the player.")]
    [SerializeField] private float m_slowDuration = 0.25f;
    [Tooltip("Target Time.timeScale during the death cinematic. 0 freezes the world entirely; the cinematic itself runs unscaled and is unaffected.")]
    [SerializeField] private float m_haltTimeScale = 0f;

    [Header("Death Animation")]
    [Tooltip("How long to wait for the boss's death animation to play out before returning to pool. Should match the death animation clip length.")]
    [SerializeField] private float m_deathAnimationDuration = 0.5f;
    [Tooltip("Time scale during the death animation. Values below 1 create a slow-motion effect for the explosion.")]
    [SerializeField] private float m_deathAnimationTimeScale = 0.3f;

    [Header("Camera Blend-Out")]
    [Tooltip("How long the camera blends from the boss vcam back to the player vcam after the explosion.")]
    [SerializeField] private float m_blendOutDuration = 1.5f;

    private float m_originalFixedDeltaTime;
    private bool m_isDying;
    private RoomManager m_roomManager;
    private readonly List<MonoBehaviour> m_dormantBehaviours = new List<MonoBehaviour>();

    private void Awake()
    {
        m_originalFixedDeltaTime = Time.fixedDeltaTime;
    }

    private void OnEnable()
    {
        m_isDying = false;
        if (m_health != null)
            m_health.DeathInterceptor = OnLethalHit;
    }

    private void OnDisable()
    {
        if (m_health != null && m_health.DeathInterceptor == OnLethalHit)
            m_health.DeathInterceptor = null;
    }

    private bool OnLethalHit(int incomingDamage)
    {
        // Already mid-cutscene: the post-cutscene Kill() needs to fall through.
        if (m_isDying) return false;

        m_isDying = true;

        // Resolve once on first lethal hit. Needed synchronously here because we want
        // to clear minions through the encounter before returning, not from the coroutine
        // (which doesn't run until the next frame).
        if (m_roomManager == null)
            ServiceLocator.Global.TryGet(out m_roomManager);

        // Halt boss behaviours synchronously *before* returning. EntityHealth.TakeDamage
        // is about to fire OnHealthChanged(1) right after we return, and BossPhaseManager
        // is subscribed to that event — if it's still enabled it will interpret the
        // clamped 1 HP as crossing into the final phase and trigger SpawnMinions. Halting
        // here unsubscribes it via OnDisable before the event fires.
        HaltBossBehaviours();

        // Sweep any surviving minions out of the room so the cutscene plays in a clean
        // arena and the room-clear chain only has to wait on the boss itself.
        m_roomManager?.CurrentEncounter?.ClearNonBossEnemies();

        StartCoroutine(DeathSequence());
        return true;
    }

    private IEnumerator DeathSequence()
    {
        if (m_roomManager != null)
        {
            m_roomManager.SetPlayerInputActive(false);
            m_roomManager.SetPlayerGodMode(true);
            m_roomManager.SetCameraConfinerActive(false);
            m_roomManager.SetBossVcamActive(true);
        }

        float originalTimeScale = Time.timeScale;
        float elapsed = 0f;
        while (elapsed < m_slowDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / m_slowDuration);
            Time.timeScale = Mathf.Lerp(originalTimeScale, m_haltTimeScale, t);
            Time.fixedDeltaTime = m_originalFixedDeltaTime * Time.timeScale;
            yield return null;
        }
        Time.timeScale = m_haltTimeScale;
        Time.fixedDeltaTime = m_originalFixedDeltaTime * Time.timeScale;

        CinematicPlayer cinematicPlayer = null;
        if (m_deathCinematic != null
            && ServiceLocator.Global.TryGet(out cinematicPlayer))
        {
            // Bind the death timeline's CinemachineShot clips to the boss vcam at runtime.
            // Without this, the timeline's exposed-reference lookups resolve to null
            // (the director's inspector bindings only know about the intro timeline) and
            // the brain has no driving vcam, so the camera renders from (0,0,0).
            if (m_roomManager != null && m_roomManager.BossVcam != null)
                cinematicPlayer.BindAllCinemachineShots(m_deathCinematic, m_roomManager.BossVcam);

            // PlayAndHold keeps the playable graph alive at the last frame so
            // track-driven effects (letterbox bars) stay in place through the explosion.
            yield return cinematicPlayer.PlayAndHold(m_deathCinematic);
        }

        // Ramp time to a slow-mo rate for the death animation so the explosion reads as
        // the cinematic's finale. Using a nonzero timeScale (rather than 0 + unscaled
        // animators) keeps the CinemachineBrain able to process blends — with timeScale 0
        // and IgnoreTimeScale off, pending blends queue up and all resolve at once when
        // time restores, causing the camera to bounce.
        Time.timeScale = m_deathAnimationTimeScale;
        Time.fixedDeltaTime = m_originalFixedDeltaTime * m_deathAnimationTimeScale;

        if (m_animationController != null)
            m_animationController.PlayDeathAnimation();

        // The animation clip plays at the slowed rate, so real-world wait = clip / scale.
        yield return new WaitForSecondsRealtime(m_deathAnimationDuration / m_deathAnimationTimeScale);

        // Release the held timeline — this tears down the playable graph, which lets
        // TransformMoveMixerBehaviour.OnPlayableDestroy restore the bar positions.
        if (cinematicPlayer != null)
            cinematicPlayer.ReleaseHold();

        Time.timeScale = 1f;
        Time.fixedDeltaTime = m_originalFixedDeltaTime;

        // Override the brain's default blend so the transition from boss vcam back to
        // the player vcam is a long, smooth ease rather than an abrupt cut/snap.
        CinemachineBrain brain = CinemachineBrain.GetActiveBrain(0);
        CinemachineBlendDefinition originalBlend = default;
        bool brainFound = brain != null;
        if (brainFound)
        {
            originalBlend = brain.DefaultBlend;
            brain.DefaultBlend = new CinemachineBlendDefinition(
                CinemachineBlendDefinition.Styles.EaseInOut, m_blendOutDuration);
        }

        if (m_roomManager != null)
        {
            m_roomManager.SetBossVcamActive(false);
            m_roomManager.SetCameraConfinerActive(true);
        }

        // Wait for the blend to finish before restoring input so the player isn't
        // distracted by the camera still moving.
        yield return new WaitForSeconds(m_blendOutDuration);

        if (brainFound)
            brain.DefaultBlend = originalBlend;

        if (m_roomManager != null)
        {
            m_roomManager.SetPlayerInputActive(true);
            m_roomManager.SetPlayerGodMode(false);
        }

        // Clear the interceptor so the final Kill() falls through to OnDeath, which is
        // what RoomEncounter is subscribed to for room-clear bookkeeping.
        // The boss vcam has BlendHint=FreezeWhenBlendingOut on its CinemachineCamera,
        // so the brain snapshots its state at the moment SetBossVcamActive(false)
        // disables it. The snapshot is frozen at the death position, which means the
        // boss GameObject's pool teleport to (0,0,0) immediately below cannot affect
        // the in-progress boss→player blend.
        m_health.DeathInterceptor = null;
        m_health.Kill();
        if (m_enemyController != null)
            m_enemyController.ReturnToPool();

        // Restore enabled-state *after* the pool deactivates the GameObject. Doing it
        // earlier re-fires BossPhaseManager.OnEnable while health is still clamped at 1,
        // which makes EvaluatePhase walk through every remaining phase and SpawnMinions
        // for any with SummonsMinions=true. With the GameObject inactive, setting
        // enabled=true is a no-op until the next pool fetch reactivates the boss and
        // runs a fresh OnEnable against full health.
        RestoreBossBehaviours();
    }

    private void HaltBossBehaviours()
    {
        m_dormantBehaviours.Clear();
        foreach (MonoBehaviour mb in GetComponentsInChildren<MonoBehaviour>())
        {
            if (mb == this) continue;
            // EnemyAnimationController must keep ticking so PlayDeathAnimation lands on
            // a live animator pipeline once the cinematic ends.
            if (mb is EnemyAnimationController) continue;
            if (!mb.enabled) continue;

            mb.enabled = false;
            m_dormantBehaviours.Add(mb);
        }
    }

    private void RestoreBossBehaviours()
    {
        foreach (MonoBehaviour mb in m_dormantBehaviours)
        {
            if (mb != null)
                mb.enabled = true;
        }
        m_dormantBehaviours.Clear();
    }
}
