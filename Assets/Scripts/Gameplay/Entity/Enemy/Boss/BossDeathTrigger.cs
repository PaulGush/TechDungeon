using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Timeline;
using UnityServiceLocator;

/// <summary>
/// Thin trigger script for the boss death cutscene. Intercepts the lethal hit,
/// does synchronous cleanup (halt behaviours, clear minions), then hands off to
/// the death timeline which drives all choreography via custom tracks.
/// <para>
/// The timeline calls back into this script's public methods via
/// <see cref="BossEventTrack"/> clips (PlayDeathAnimation, Kill, ReturnToPool).
/// </para>
/// </summary>
public class BossDeathTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EntityHealth m_health;
    [SerializeField] private EnemyController m_enemyController;
    [SerializeField] private EnemyAnimationController m_animationController;
    [SerializeField] private TimelineAsset m_deathTimeline;

    [Header("Timeline Clip Names")]
    [Tooltip("Display name of the CinemachineShot clips on the death timeline that should be bound to the boss vcam.")]
    [SerializeField] private string m_bossCameraClipName = "Boss Cam";
    [Tooltip("Display name of the CinemachineShot clips on the death timeline that should be bound to the player vcam.")]
    [SerializeField] private string m_playerCameraClipName = "Player Cam";

    private bool m_isDying;
    private RoomManager m_roomManager;
    private readonly List<MonoBehaviour> m_dormantBehaviours = new List<MonoBehaviour>();

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
        if (m_isDying) return false;

        m_isDying = true;

        if (m_roomManager == null)
            ServiceLocator.Global.TryGet(out m_roomManager);

        // Halt boss behaviours synchronously before returning. TakeDamage is about
        // to fire OnHealthChanged(1) and BossPhaseManager would interpret the clamped
        // 1 HP as crossing into the final phase and spawn minions.
        HaltBossBehaviours();

        m_roomManager?.CurrentEncounter?.ClearNonBossEnemies();

        // Bind and play the death timeline. All choreography (time scale, game state,
        // camera, animation triggers, kill) is driven by the timeline's custom tracks.
        if (m_deathTimeline != null
            && ServiceLocator.Global.TryGet(out CinematicPlayer cinematicPlayer))
        {
            BindTracks(cinematicPlayer);
            cinematicPlayer.Play(m_deathTimeline);
        }

        return true;
    }

    private void BindTracks(CinematicPlayer cinematicPlayer)
    {
        // CinemachineShots need runtime binding so the vcams resolve correctly.
        if (m_roomManager != null)
        {
            if (m_roomManager.BossVcam != null)
                cinematicPlayer.BindCinemachineShots(m_deathTimeline, m_bossCameraClipName, m_roomManager.BossVcam);

            if (m_roomManager.PlayerVcam != null)
                cinematicPlayer.BindCinemachineShots(m_deathTimeline, m_playerCameraClipName, m_roomManager.PlayerVcam);
        }

        // Bind custom tracks to their targets at runtime.
        cinematicPlayer.BindTrack<GameStateTrack>(m_deathTimeline, m_roomManager);
        cinematicPlayer.BindTrack<CameraBlendTrack>(m_deathTimeline, CinemachineBrain.GetActiveBrain(0));
        cinematicPlayer.BindTrack<BossEventTrack>(m_deathTimeline, this);
    }

    // ---- Callbacks invoked by BossEventTrack clips ----

    public void PlayDeathAnimation()
    {
        if (m_animationController != null)
            m_animationController.PlayDeathAnimation();
    }

    public void Kill()
    {
        m_health.DeathInterceptor = null;
        m_health.Kill();
    }

    public void ReturnToPool()
    {
        if (m_enemyController != null)
            m_enemyController.ReturnToPool();

        RestoreBossBehaviours();
    }

    // ---- Behaviour suspension ----

    private void HaltBossBehaviours()
    {
        m_dormantBehaviours.Clear();
        foreach (MonoBehaviour mb in GetComponentsInChildren<MonoBehaviour>())
        {
            if (mb == this) continue;
            if (mb is EnemyAnimationController) continue;
            if (!mb.enabled) continue;

            mb.enabled = false;
            m_dormantBehaviours.Add(mb);
        }

        // Stop the boss in its tracks. The animation controller stays enabled and
        // reads IsMoving each frame to set the Running animator bool — we need to
        // ensure that reads false. Freezing before disable means FixedUpdate won't
        // run to clear it, so also zero the rigidbody velocity directly.
        EnemyMovement movement = m_enemyController.Movement;
        if (movement != null)
        {
            movement.Frozen = true;
            movement.Stop();
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
