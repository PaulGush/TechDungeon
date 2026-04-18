using Unity.Cinemachine;
using UnityEngine;
using UnityServiceLocator;

public class CameraShake : MonoBehaviour
{
    [Tooltip("Impulse source that generates camera shake events. Each vcam needs a CinemachineImpulseListener to receive them — shake shape, decay, and frequency are authored on the source's Impulse Definition.")]
    [SerializeField] private CinemachineImpulseSource m_impulseSource;

    [Header("Throttling")]
    [Tooltip("Minimum unscaled seconds between impulses below the always-fire threshold. Prevents rapid-fire small shakes (shoot recoil, cluster hits) from stacking into a chaotic blur.")]
    [SerializeField] private float m_minInterval = 0.05f;

    [Tooltip("Shakes at or above this amplitude always fire and ignore the cooldown. Lets big events (player damage, explosion, boss death) punch through rapid-fire chatter.")]
    [SerializeField] private float m_alwaysFireAmplitude = 0.2f;

    private float m_nextAllowedTime;

    private void Awake()
    {
        ServiceLocator.Global.Register(this);
    }

    public void Shake(float amplitude)
    {
        // Callers without a directional context get a random kick so repeated shakes
        // don't all push the camera along the same vector.
        float angle = Random.Range(0f, Mathf.PI * 2f);
        Shake(amplitude, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
    }

    public void Shake(float amplitude, Vector2 direction)
    {
        if (m_impulseSource == null)
        {
            Debug.LogWarning($"{nameof(CameraShake)}: {nameof(m_impulseSource)} is not assigned.", this);
            return;
        }

        if (amplitude < m_alwaysFireAmplitude && Time.unscaledTime < m_nextAllowedTime) return;
        m_nextAllowedTime = Time.unscaledTime + m_minInterval;

        Vector2 unit = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;
        m_impulseSource.GenerateImpulseWithVelocity(new Vector3(unit.x, unit.y, 0f) * amplitude);
    }
}
