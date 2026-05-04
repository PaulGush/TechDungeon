using UI.InputPrompts;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
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

    [Header("Gamepad Rumble")]
    [SerializeField] private bool m_enableRumble = true;
    [Tooltip("Multiplier applied to shake amplitude to get motor speed (0..1). Higher = stronger rumble per shake.")]
    [SerializeField] private float m_rumbleScale = 1f;
    [Tooltip("Per-second linear decay of accumulated rumble.")]
    [SerializeField] private float m_rumbleDecay = 4f;
    [Tooltip("Caps stacked rumble so rapid impulses can't pin motors at full power.")]
    [SerializeField, Range(0f, 1f)] private float m_rumbleMax = 1f;

    private float m_nextAllowedTime;
    private float m_currentRumble;
    private float m_sustainedRumble;
    private bool m_motorsActive;

    private void Awake()
    {
        ServiceLocator.Global.Register(this);
    }

    private void OnEnable()
    {
        ActiveDeviceTracker.DeviceChanged += OnActiveDeviceChanged;
    }

    private void OnDisable()
    {
        ActiveDeviceTracker.DeviceChanged -= OnActiveDeviceChanged;
        StopMotors();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) StopMotors();
    }

    private void Update()
    {
        if (m_currentRumble > 0f)
            m_currentRumble = Mathf.Max(0f, m_currentRumble - m_rumbleDecay * Time.unscaledDeltaTime);

        float total = Mathf.Min(m_rumbleMax, m_currentRumble + m_sustainedRumble);
        if (total <= 0f)
        {
            if (m_motorsActive) StopMotors();
            return;
        }
        ApplyMotors(total);
    }

    public void Shake(float amplitude)
    {
        // Callers without a directional context get a random kick so repeated shakes
        // don't all push the camera along the same vector.
        float angle = Random.Range(0f, Mathf.PI * 2f);
        Shake(amplitude, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), 1f);
    }

    public void Shake(float amplitude, Vector2 direction)
    {
        Shake(amplitude, direction, 1f);
    }

    public void Shake(float amplitude, Vector2 direction, float rumbleMultiplier)
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
        AddRumble(amplitude * rumbleMultiplier);
    }

    private void AddRumble(float scaledAmplitude)
    {
        if (!m_enableRumble) return;
        if (ActiveDeviceTracker.Current == ActiveDevice.KeyboardMouse) return;
        float add = scaledAmplitude * m_rumbleScale;
        if (add <= 0f) return;
        m_currentRumble = Mathf.Min(m_rumbleMax, m_currentRumble + add);
    }

    /// <summary>
    /// One-shot rumble pulse without generating a camera shake impulse. Use for events that should
    /// rattle the gamepad but not the screen (e.g. boss footsteps, distant explosions).
    /// </summary>
    public void Rumble(float amount)
    {
        AddRumble(amount);
    }

    /// <summary>
    /// Set a held rumble level (0..1) summed on top of impulse-driven rumble. Call every frame
    /// while sustaining (e.g. weapon charging) and call <see cref="ClearSustainedRumble"/> when done.
    /// </summary>
    public void SetSustainedRumble(float amount)
    {
        if (!m_enableRumble || ActiveDeviceTracker.Current == ActiveDevice.KeyboardMouse)
        {
            m_sustainedRumble = 0f;
            return;
        }
        m_sustainedRumble = Mathf.Clamp(amount, 0f, m_rumbleMax);
    }

    public void ClearSustainedRumble()
    {
        m_sustainedRumble = 0f;
    }

    private void ApplyMotors(float intensity)
    {
        Gamepad pad = Gamepad.current;
        if (pad == null) return;
        pad.SetMotorSpeeds(intensity, intensity);
        m_motorsActive = true;
    }

    private void StopMotors()
    {
        Gamepad pad = Gamepad.current;
        if (pad != null) pad.SetMotorSpeeds(0f, 0f);
        m_motorsActive = false;
        m_currentRumble = 0f;
        m_sustainedRumble = 0f;
    }

    private void OnActiveDeviceChanged(ActiveDevice device)
    {
        if (device == ActiveDevice.KeyboardMouse) StopMotors();
    }
}
