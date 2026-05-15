using System.Collections.Generic;
using UnityEngine;

public class Flamethrower : MonoBehaviour
{
    [SerializeField] private Collider2D m_hitbox;
    [SerializeField] private LayerMask m_damageLayers;

    private int m_damagePerTick;
    private float m_tickInterval;
    private float m_duration;
    private float m_activeTimer;
    private float m_tickTimer;
    private bool m_isFiring;

    private readonly HashSet<EntityHealth> m_targets = new HashSet<EntityHealth>();
    private readonly List<EntityHealth> m_deadTargetsBuffer = new List<EntityHealth>();

    public void Fire(int damagePerTick, float tickInterval, float duration)
    {
        m_damagePerTick = damagePerTick;
        m_tickInterval = tickInterval;
        m_duration = duration;
        m_activeTimer = 0f;
        m_tickTimer = 0f;
        m_isFiring = true;
        m_targets.Clear();

        m_hitbox.enabled = true;
    }

    public void Stop()
    {
        m_isFiring = false;
        m_hitbox.enabled = false;
        m_targets.Clear();
    }

    private void OnDisable()
    {
        Stop();
    }

    private void Update()
    {
        if (!m_isFiring) return;

        m_activeTimer += Time.deltaTime;
        if (m_activeTimer >= m_duration)
        {
            Stop();
            return;
        }

        m_tickTimer += Time.deltaTime;
        if (m_tickTimer >= m_tickInterval)
        {
            m_tickTimer -= m_tickInterval;
            DamageTick();
        }
    }

    private void DamageTick()
    {
        foreach (EntityHealth target in m_targets)
        {
            if (target == null || target.IsDead)
            {
                m_deadTargetsBuffer.Add(target);
                continue;
            }

            target.TakeDamage(m_damagePerTick);
        }

        if (m_deadTargetsBuffer.Count == 0) return;

        for (int i = 0; i < m_deadTargetsBuffer.Count; i++)
            m_targets.Remove(m_deadTargetsBuffer[i]);
        m_deadTargetsBuffer.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!m_isFiring) return;
        if (((1 << other.gameObject.layer) & m_damageLayers) == 0) return;

        if (other.TryGetComponent(out EntityHealth health))
            m_targets.Add(health);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out EntityHealth health))
            m_targets.Remove(health);
    }
}
