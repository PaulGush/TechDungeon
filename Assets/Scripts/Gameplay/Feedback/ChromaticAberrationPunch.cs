using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ChromaticAberrationPunch : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Damage event source. The punch fires whenever this EntityHealth reports a hit.")]
    [SerializeField] private EntityHealth m_health;

    [Header("Feel")]
    [Tooltip("Chromatic Aberration intensity applied at the instant of the hit.")]
    [Range(0f, 1f)]
    [SerializeField] private float m_peakIntensity = 0.7f;

    [Tooltip("Seconds for the intensity to ease from Peak back down to zero. Once zero, the script releases its override so the project's default baseline CA shows through again.")]
    [SerializeField] private float m_recoverDuration = 0.35f;

    [Tooltip("Priority of the runtime Volume. Higher than any authored scene volumes so the punch wins.")]
    [SerializeField] private float m_volumePriority = 100f;

    private Volume m_volume;
    private ChromaticAberration m_ca;
    private Coroutine m_routine;

    private void Awake()
    {
        // Build a dedicated runtime Volume so we don't stomp the project's shared profile.
        GameObject volumeObject = new GameObject("ChromaticAberrationPunchVolume");
        volumeObject.transform.SetParent(transform, false);
        m_volume = volumeObject.AddComponent<Volume>();
        m_volume.isGlobal = true;
        m_volume.priority = m_volumePriority;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "ChromaticAberrationPunchProfile";
        m_ca = profile.Add<ChromaticAberration>(overrides: true);
        m_ca.intensity.overrideState = false;
        m_ca.intensity.value = 0f;
        m_volume.sharedProfile = profile;
    }

    private void OnEnable()
    {
        if (m_health != null)
            m_health.OnTakeDamage += HandleHit;
    }

    private void OnDisable()
    {
        if (m_health != null)
            m_health.OnTakeDamage -= HandleHit;

        if (m_routine != null)
        {
            StopCoroutine(m_routine);
            m_routine = null;
        }
        if (m_ca != null)
            m_ca.intensity.overrideState = false;
    }

    private void HandleHit()
    {
        if (m_routine != null) StopCoroutine(m_routine);
        m_routine = StartCoroutine(PunchRoutine());
    }

    private IEnumerator PunchRoutine()
    {
        m_ca.intensity.overrideState = true;
        m_ca.intensity.value = m_peakIntensity;

        float elapsed = 0f;
        while (elapsed < m_recoverDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / m_recoverDuration);
            // Ease-out so most of the intensity falls off early and the tail is short.
            float eased = 1f - (1f - t) * (1f - t);
            m_ca.intensity.value = Mathf.Lerp(m_peakIntensity, 0f, eased);
            yield return null;
        }

        m_ca.intensity.value = 0f;
        // Release the override so the default profile's baseline CA reads through again.
        m_ca.intensity.overrideState = false;
        m_routine = null;
    }
}
