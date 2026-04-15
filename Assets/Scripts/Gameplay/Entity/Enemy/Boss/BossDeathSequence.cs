using System.Collections;
using System.Collections.Generic;
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

        if (m_deathCinematic != null
            && ServiceLocator.Global.TryGet(out CinematicPlayer cinematicPlayer))
        {
            // Bind the death timeline's CinemachineShot clips to the boss vcam at runtime.
            // Without this, the timeline's exposed-reference lookups resolve to null
            // (the director's inspector bindings only know about the intro timeline) and
            // the brain has no driving vcam, so the camera renders from (0,0,0).
            if (m_roomManager != null && m_roomManager.BossVcam != null)
                cinematicPlayer.BindAllCinemachineShots(m_deathCinematic, m_roomManager.BossVcam);

            yield return cinematicPlayer.Play(m_deathCinematic);
        }

        // Restore time before the explosion so the death animation reads at full speed.
        Time.timeScale = 1f;
        Time.fixedDeltaTime = m_originalFixedDeltaTime;

        // Death animation contains the explosion VFX baked in. Trigger it manually since
        // the default OnDeath subscription is suppressed on this entity. Camera stays on
        // the boss vcam through the explosion for framing.
        if (m_animationController != null)
            m_animationController.PlayDeathAnimation();

        yield return new WaitForSecondsRealtime(m_deathAnimationDuration);

        if (m_roomManager != null)
        {
            m_roomManager.SetBossVcamActive(false);
            m_roomManager.SetCameraConfinerActive(true);
            m_roomManager.SetPlayerInputActive(true);
            m_roomManager.SetPlayerGodMode(false);
        }

        // Re-enable any behaviours we paused so pool reuse spawns a clean boss next run.
        RestoreBossBehaviours();

        // Clear the interceptor so the final Kill() falls through to OnDeath, which is
        // what RoomEncounter is subscribed to for room-clear bookkeeping.
        m_health.DeathInterceptor = null;
        m_health.Kill();
        if (m_enemyController != null)
            m_enemyController.ReturnToPool();
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
