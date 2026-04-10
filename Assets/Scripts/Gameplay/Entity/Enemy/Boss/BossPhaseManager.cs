using System;
using System.Collections.Generic;
using Gameplay.ObjectPool;
using UnityEngine;
using UnityServiceLocator;

public class BossPhaseManager : MonoBehaviour
{
    [SerializeField] private EntityHealth m_health;
    [SerializeField] private EnemyAnimationController m_animationController;
    [SerializeField] private EnemyMovement m_movement;
    [SerializeField] private BossSettings m_settings;

    private BossSettings m_runtimeSettings;
    private int m_currentPhaseIndex;
    private bool m_initialized;

    public BossPhase CurrentPhase => m_settings.Phases[m_currentPhaseIndex];
    public int CurrentPhaseIndex => m_currentPhaseIndex;
    public event Action<int> OnPhaseChanged;

    private void OnEnable()
    {
        m_currentPhaseIndex = 0;
        m_initialized = false;

        if (m_runtimeSettings == null)
        {
            m_runtimeSettings = Instantiate(m_settings);
            m_movement.SetRuntimeSettings(m_runtimeSettings);
        }

        ApplyPhaseSettings(m_settings.Phases[0]);

        if (m_health != null)
        {
            m_health.OnHealthChanged += EvaluatePhase;
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

        // Find the highest phase index whose threshold we've crossed
        int targetPhase = 0;
        for (int i = 0; i < m_settings.Phases.Count; i++)
        {
            if (healthPercent <= m_settings.Phases[i].HealthThreshold)
            {
                targetPhase = i;
            }
        }

        if (!m_initialized)
        {
            m_currentPhaseIndex = targetPhase;
            m_initialized = true;
            if (m_animationController != null)
                m_animationController.CurrentAttackIndex = m_currentPhaseIndex;
            OnPhaseChanged?.Invoke(m_currentPhaseIndex);
            return;
        }

        if (targetPhase <= m_currentPhaseIndex) return;

        m_currentPhaseIndex = targetPhase;
        ApplyPhaseSettings(CurrentPhase);
        if (m_animationController != null)
            m_animationController.CurrentAttackIndex = m_currentPhaseIndex;
        OnPhaseChanged?.Invoke(m_currentPhaseIndex);

        if (CurrentPhase.SummonsMinions)
        {
            SpawnMinions(CurrentPhase);
        }
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
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 2f;
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
