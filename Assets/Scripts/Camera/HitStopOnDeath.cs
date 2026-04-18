using System.Collections;
using UnityEngine;
using UnityServiceLocator;

public class HitStopOnDeath : MonoBehaviour
{
    [SerializeField] private EntityHealth m_health;

    [Tooltip("Unscaled seconds to hold the freeze when this entity dies. 0.05 is subtle, 0.10 is a clear punch, 0.15+ reads as heavy/boss-grade.")]
    [SerializeField] private float m_duration = 0.1f;

    [Tooltip("Time.timeScale during the freeze. 0 = full stop, small values (0.02–0.1) read as a satisfying punch.")]
    [SerializeField, Range(0f, 1f)] private float m_scale = 0.02f;

    [Tooltip("Unscaled seconds to wait after OnDeath before triggering the freeze. Lets the death animation play partway so the hit-stop lands on the impact beat rather than the start of the anim.")]
    [SerializeField] private float m_delay;

    private HitStopService m_service;
    private Coroutine m_pendingFreeze;

    private void OnEnable()
    {
        if (m_health != null)
            m_health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (m_health != null)
            m_health.OnDeath -= HandleDeath;

        if (m_pendingFreeze != null)
        {
            StopCoroutine(m_pendingFreeze);
            m_pendingFreeze = null;
        }
    }

    private void HandleDeath()
    {
        if (m_delay <= 0f)
        {
            Freeze();
            return;
        }

        if (m_pendingFreeze != null) StopCoroutine(m_pendingFreeze);
        m_pendingFreeze = StartCoroutine(DelayedFreeze());
    }

    private IEnumerator DelayedFreeze()
    {
        yield return new WaitForSecondsRealtime(m_delay);
        m_pendingFreeze = null;
        Freeze();
    }

    private void Freeze()
    {
        if (m_service == null)
            ServiceLocator.Global.TryGet(out m_service);

        m_service?.Freeze(m_duration, m_scale);
    }
}
