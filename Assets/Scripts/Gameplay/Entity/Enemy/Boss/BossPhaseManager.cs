using System;
using System.Collections.Generic;
using Gameplay.ObjectPool;
using UnityEngine;
using UnityServiceLocator;

public class BossPhaseManager : MonoBehaviour
{
    // Radius at which summoned minions spawn around the boss, as a multiple of the unit circle.
    private const float MinionSpawnRadius = 2f;

    [SerializeField] private EntityHealth m_health;
    [SerializeField] private EnemyAnimationController m_animationController;
    [SerializeField] private EnemyMovement m_movement;
    [SerializeField] private EnemyShooting m_shooting;
    [SerializeField] private BossSettings m_settings;

    private BossSettings m_runtimeSettings;
    private int m_currentPhaseIndex;

    public BossPhase CurrentPhase =>
        m_settings != null && m_settings.Phases != null && m_settings.Phases.Count > 0
            ? m_settings.Phases[m_currentPhaseIndex]
            : null;
    public int CurrentPhaseIndex => m_currentPhaseIndex;
    public event Action<int> OnPhaseChanged;

    private void OnEnable()
    {
        m_currentPhaseIndex = 0;

        if (m_settings == null || m_settings.Phases == null || m_settings.Phases.Count == 0)
        {
            Debug.LogError($"{nameof(BossPhaseManager)}: BossSettings has no phases configured.", this);
            return;
        }

        if (m_runtimeSettings == null)
        {
            m_runtimeSettings = Instantiate(m_settings);
            m_movement.SetRuntimeSettings(m_runtimeSettings);
            if (m_shooting != null)
                m_shooting.SetRuntimeSettings(m_runtimeSettings);
        }

        ApplyPhaseSettings(m_settings.Phases[0]);
        SyncAttackAnimationIndex();

        if (m_health != null)
        {
            m_health.OnHealthChanged += EvaluatePhase;
            // Edge case: if the boss spawns already below phase 0's threshold
            // (pre-damaged state, revives, etc.), sync to the correct starting phase.
            EvaluatePhase(m_health.CurrentHealth);
        }
    }

    private void OnDisable()
    {
        if (m_health != null)
        {
            m_health.OnHealthChanged -= EvaluatePhase;
        }
    }

    private void EvaluatePhase(int currentHealth)
    {
        if (m_settings.Phases == null || m_settings.Phases.Count == 0) return;

        float healthPercent = (float)currentHealth / m_health.MaxHealth;

        // Advance phases strictly sequentially. If a single damage event crosses multiple
        // thresholds (e.g. a huge hit), loop until the boss sits in the correct phase —
        // but run ApplyPhaseSettings / OnPhaseChanged / SpawnMinions for every phase it
        // passes through so none are silently skipped.
        while (m_currentPhaseIndex + 1 < m_settings.Phases.Count &&
               healthPercent <= m_settings.Phases[m_currentPhaseIndex + 1].HealthThreshold)
        {
            m_currentPhaseIndex++;
            ApplyPhaseSettings(CurrentPhase);
            SyncAttackAnimationIndex();
            OnPhaseChanged?.Invoke(m_currentPhaseIndex);

            if (CurrentPhase.SummonsMinions)
                SpawnMinions(CurrentPhase);
        }
    }

    private void SyncAttackAnimationIndex()
    {
        if (m_animationController == null || CurrentPhase == null) return;
        m_animationController.CurrentAttackIndex = CurrentPhase.AttackAnimationIndex;
    }

    private void ApplyPhaseSettings(BossPhase phase)
    {
        // Reset to base values first
        m_runtimeSettings.Speed = m_settings.Speed;
        m_runtimeSettings.StrafeSpeed = m_settings.StrafeSpeed;
        m_runtimeSettings.AttackRange = m_settings.AttackRange;
        m_runtimeSettings.PreferredAttackDistance = m_settings.PreferredAttackDistance;
        m_runtimeSettings.FireRate = m_settings.FireRate;

        // Apply overrides
        if (phase.SpeedOverride >= 0) m_runtimeSettings.Speed = phase.SpeedOverride;
        if (phase.StrafeSpeedOverride >= 0) m_runtimeSettings.StrafeSpeed = phase.StrafeSpeedOverride;
        if (phase.AttackRangeOverride >= 0) m_runtimeSettings.AttackRange = phase.AttackRangeOverride;
        if (phase.PreferredAttackDistanceOverride >= 0) m_runtimeSettings.PreferredAttackDistance = phase.PreferredAttackDistanceOverride;
        if (phase.FireRateOverride >= 0) m_runtimeSettings.FireRate = phase.FireRateOverride;
    }

    private void SpawnMinions(BossPhase phase)
    {
        if (phase.MinionPrefab == null || phase.MinionCount <= 0) return;

        ObjectPool pool = null;
        ServiceLocator.Global.TryGet(out pool);

        RoomEncounter encounter = GetComponentInParent<RoomEncounter>();
        if (encounter == null)
        {
            encounter = transform.root.GetComponentInChildren<RoomEncounter>();
        }

        for (int i = 0; i < phase.MinionCount; i++)
        {
            float angle = (360f / phase.MinionCount) * i * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * MinionSpawnRadius;
            Vector3 spawnPos = transform.position + (Vector3)offset;

            GameObject minion;
            if (pool != null)
            {
                minion = pool.GetPooledObject(phase.MinionPrefab);
                minion.transform.position = spawnPos;
            }
            else
            {
                minion = Instantiate(phase.MinionPrefab, spawnPos, Quaternion.identity);
            }

            if (encounter != null)
            {
                encounter.RegisterEnemy(minion);
            }
        }
    }
}
