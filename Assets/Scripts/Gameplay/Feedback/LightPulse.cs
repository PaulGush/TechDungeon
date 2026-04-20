using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class LightPulse : MonoBehaviour
{
    [Tooltip("Intensity the light holds at the start of the pulse. Fades to zero over Duration.")]
    [SerializeField] private float m_peakIntensity = 2f;

    [Tooltip("Seconds for the pulse to fade from PeakIntensity to zero.")]
    [SerializeField] private float m_duration = 0.15f;

    [Tooltip("If true, the pulse starts automatically every time this component is enabled (ideal for pooled explosions and toggled muzzle flashes).")]
    [SerializeField] private bool m_pulseOnEnable = true;

    [Tooltip("If true, timing ignores Time.timeScale so the pulse reads clearly during hit-stop freezes.")]
    [SerializeField] private bool m_useUnscaledTime = true;

    private Light2D m_light;
    private Coroutine m_routine;

    private void Awake()
    {
        m_light = GetComponent<Light2D>();
    }

    private void OnEnable()
    {
        if (m_pulseOnEnable)
            Pulse();
    }

    private void OnDisable()
    {
        if (m_routine != null)
        {
            StopCoroutine(m_routine);
            m_routine = null;
        }
        if (m_light != null)
            m_light.intensity = 0f;
    }

    public void Pulse()
    {
        if (m_routine != null) StopCoroutine(m_routine);
        m_routine = StartCoroutine(PulseRoutine());
    }

    private IEnumerator PulseRoutine()
    {
        if (m_duration <= 0f)
        {
            m_light.intensity = 0f;
            m_routine = null;
            yield break;
        }

        float elapsed = 0f;
        m_light.intensity = m_peakIntensity;

        while (elapsed < m_duration)
        {
            elapsed += m_useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / m_duration);
            // Ease-out: fast fall early, long tail.
            float eased = 1f - (1f - t) * (1f - t);
            m_light.intensity = Mathf.Lerp(m_peakIntensity, 0f, eased);
            yield return null;
        }

        m_light.intensity = 0f;
        m_routine = null;
    }
}
