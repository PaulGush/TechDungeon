using System.Collections;
using UnityEngine;
using UnityServiceLocator;

public class HitStopService : MonoBehaviour
{
    [Tooltip("Hard cap on how long a single Freeze call may hold. Guards against runaway requests.")]
    [SerializeField] private float m_maxDuration = 0.3f;

    private Coroutine m_coroutine;
    private float m_activeEndTime;
    private float m_preFreezeTimeScale = 1f;

    private void Awake()
    {
        ServiceLocator.Global.Register(this);
    }

    public void Freeze(float seconds, float scale = 0.02f)
    {
        if (seconds <= 0f) return;

        seconds = Mathf.Min(seconds, m_maxDuration);
        float newEnd = Time.unscaledTime + seconds;

        // If a freeze is already active and its end time is later, skip this weaker request.
        if (newEnd <= m_activeEndTime) return;

        m_activeEndTime = newEnd;

        if (m_coroutine == null)
        {
            m_preFreezeTimeScale = Time.timeScale;
            Time.timeScale = scale;
            m_coroutine = StartCoroutine(FreezeRoutine());
        }
    }

    private IEnumerator FreezeRoutine()
    {
        while (Time.unscaledTime < m_activeEndTime)
            yield return null;

        Time.timeScale = m_preFreezeTimeScale;
        m_coroutine = null;
    }

    private void OnDisable()
    {
        if (m_coroutine != null)
        {
            StopCoroutine(m_coroutine);
            m_coroutine = null;
            Time.timeScale = m_preFreezeTimeScale;
        }
    }
}
