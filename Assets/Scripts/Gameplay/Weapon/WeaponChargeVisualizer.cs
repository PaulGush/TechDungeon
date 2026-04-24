using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Drives per-weapon charge feedback: tints the weapon sprite and ramps an optional Light2D as
/// ChargeProgress grows 0→1, and punches a bright flash the moment the crit window opens by
/// writing the PixelOutline shader's _FlashAmount/_FlashColor so the flash survives the shader's
/// vertex-color multiply (which would otherwise swallow a white tint).
/// </summary>
public class WeaponChargeVisualizer : MonoBehaviour
{
    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
    private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");

    [Header("References")]
    [SerializeField] private WeaponShooting m_shooting;
    [Tooltip("SpriteRenderer whose vertex color is tinted for the charge ramp and whose material's _FlashAmount/_FlashColor are driven for the crit flash. Material must be PixelOutline or another shader exposing those properties.")]
    [SerializeField] private SpriteRenderer m_spriteRenderer;
    [Tooltip("Optional Light2D that ramps up with charge and spikes during the crit flash. Leave empty to skip lighting.")]
    [SerializeField] private Light2D m_chargeLight;

    [Header("Charge Ramp")]
    [Tooltip("Vertex-color tint blended toward at full charge. Linearly interpolated from the sprite's authored color. Pick a non-white color — the shader multiplies sprite color against the texture, so white produces no visible change.")]
    [SerializeField] private Color m_chargedTint = new Color(1f, 0.65f, 0.2f, 1f);
    [Tooltip("Light intensity at full charge, lerped from the light's authored intensity. Ignored when no Light2D is assigned.")]
    [SerializeField] private float m_chargedLightIntensity = 1.5f;

    [Header("Crit Flash")]
    [Tooltip("Color fed to the shader's _FlashColor the moment the crit window opens.")]
    [SerializeField] private Color m_critFlashColor = Color.white;
    [Tooltip("Peak _FlashAmount at crit entry. 1 fully replaces the sprite's colors with _FlashColor, 0 disables.")]
    [Range(0f, 1f)] [SerializeField] private float m_critFlashAmount = 1f;
    [Tooltip("Light intensity during the crit flash peak.")]
    [SerializeField] private float m_critFlashLightIntensity = 2.5f;
    [Tooltip("Seconds the crit-entry flash takes to decay back to the full-charge look. The rest of the crit window keeps the fully-charged tint so late releases still read as 'hot'.")]
    [SerializeField] private float m_critFlashDecay = 0.08f;

    private Color m_baseTint;
    private float m_baseLightIntensity;
    private Material m_materialInstance;
    private float m_critFlashTimer;
    private bool m_subscribed;

    private Material MaterialInstance
    {
        get
        {
            if (m_materialInstance == null && m_spriteRenderer != null)
                m_materialInstance = m_spriteRenderer.material;
            return m_materialInstance;
        }
    }

    private void Awake()
    {
        if (m_spriteRenderer != null)
            m_baseTint = m_spriteRenderer.color;
        if (m_chargeLight != null)
            m_baseLightIntensity = m_chargeLight.intensity;
    }

    private void OnEnable()
    {
        Subscribe();
        ResetVisuals();
    }

    private void OnDisable()
    {
        Unsubscribe();
        ResetVisuals();
    }

    private void Subscribe()
    {
        if (m_subscribed || m_shooting == null) return;
        m_shooting.OnCritWindowEntered += OnCritEntered;
        m_subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!m_subscribed || m_shooting == null) return;
        m_shooting.OnCritWindowEntered -= OnCritEntered;
        m_subscribed = false;
    }

    private void OnCritEntered()
    {
        m_critFlashTimer = Mathf.Max(0.0001f, m_critFlashDecay);
        Material mat = MaterialInstance;
        if (mat != null)
            mat.SetColor(FlashColorId, m_critFlashColor);
    }

    private void Update()
    {
        if (m_shooting == null) return;

        float progress = m_shooting.ChargeProgress;
        bool charging = m_shooting.IsCharging;
        bool flashing = m_critFlashTimer > 0f;

        if (!charging && !flashing)
        {
            // Only restore once per idle frame to avoid fighting other systems that may also
            // read/write the tint or flash amount while the weapon sits idle.
            if (m_spriteRenderer != null && m_spriteRenderer.color != m_baseTint)
                m_spriteRenderer.color = m_baseTint;
            if (m_chargeLight != null && !Mathf.Approximately(m_chargeLight.intensity, m_baseLightIntensity))
                m_chargeLight.intensity = m_baseLightIntensity;
            ClearFlashAmount();
            return;
        }

        // Charge ramp: vertex color tints toward m_chargedTint, light ramps toward m_chargedLightIntensity.
        if (m_spriteRenderer != null)
            m_spriteRenderer.color = Color.Lerp(m_baseTint, m_chargedTint, progress);
        float lightIntensity = Mathf.Lerp(m_baseLightIntensity, m_chargedLightIntensity, progress);

        // Crit flash: _FlashAmount starts at m_critFlashAmount and decays linearly to 0, with the
        // light intensity blended toward m_critFlashLightIntensity for the same window.
        float flashT = 0f;
        if (flashing)
        {
            m_critFlashTimer -= Time.deltaTime;
            flashT = Mathf.Clamp01(m_critFlashTimer / Mathf.Max(0.0001f, m_critFlashDecay));
            lightIntensity = Mathf.Lerp(lightIntensity, m_critFlashLightIntensity, flashT);
        }

        Material mat = MaterialInstance;
        if (mat != null)
            mat.SetFloat(FlashAmountId, flashT * m_critFlashAmount);

        if (m_chargeLight != null)
            m_chargeLight.intensity = lightIntensity;
    }

    private void ResetVisuals()
    {
        if (m_spriteRenderer != null)
            m_spriteRenderer.color = m_baseTint;
        if (m_chargeLight != null)
            m_chargeLight.intensity = m_baseLightIntensity;
        m_critFlashTimer = 0f;
        ClearFlashAmount();
    }

    private void ClearFlashAmount()
    {
        if (m_materialInstance != null)
            m_materialInstance.SetFloat(FlashAmountId, 0f);
    }
}
