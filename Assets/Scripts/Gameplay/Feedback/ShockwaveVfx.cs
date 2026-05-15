using Gameplay.ObjectPool;
using UnityEngine;
using UnityServiceLocator;

/// <summary>
/// Pooled "ripple" visual for the Shockwave ability: a short cascade of concentric rings that
/// snap outward from the cast point to the ability's blast radius, each one a little thinner and
/// a beat later than the one before, gently undulating as it goes — the energy version of a
/// stone dropped in water. Built from procedural LineRenderers it spawns as children, so there's
/// no art to maintain and the URP bloom volume does the glow.
/// <para>
/// <see cref="Play"/> sizes and colours the rings so they always match the actual damage radius
/// and the ability tint. Runs on unscaled time so the ripples keep travelling while the ability's
/// hit-stop freezes the rest of the scene.
/// </para>
/// </summary>
public class ShockwaveVfx : MonoBehaviour
{
    [Header("Ripples")]
    [Tooltip("Number of concentric rings emitted in the cascade.")]
    [SerializeField, Range(1, 6)] private int m_rippleCount = 3;

    [Tooltip("Seconds (unscaled) between each successive ring launching.")]
    [SerializeField, Min(0f)] private float m_rippleStagger = 0.06f;

    [Tooltip("Seconds (unscaled) each ring takes to expand, fade, and finish.")]
    [SerializeField, Min(0.01f)] private float m_rippleLifetime = 0.3f;

    [Tooltip("Width of the faintest echo relative to the first ring. Echoes interpolate from 1 down to this.")]
    [SerializeField, Range(0.05f, 1f)] private float m_echoWidthFalloff = 0.4f;

    [Header("Shape")]
    [Tooltip("Vertex count per ring. ~24-40 reads as a clean circle at blast scale.")]
    [SerializeField, Range(8, 64)] private int m_segments = 36;

    [Tooltip("Radius a ring starts at (its bright core), in world units, before it expands.")]
    [SerializeField, Min(0f)] private float m_startRadius = 0.35f;

    [Tooltip("Radius used if Play() isn't given an explicit one (it normally is).")]
    [SerializeField, Min(0.1f)] private float m_fallbackRadius = 4f;

    [Tooltip("Line width at a ring's birth, in world units. Tapers to zero as the ring expands.")]
    [SerializeField, Min(0f)] private float m_startWidth = 0.28f;

    [Header("Undulation")]
    [Tooltip("Peak in/out wobble of a ring's radius around its circumference, in world units. 0 = perfect circles. Decays as the ring expands.")]
    [SerializeField, Min(0f)] private float m_waveAmplitude = 0.08f;

    [Tooltip("Number of wave lobes around the circumference.")]
    [SerializeField, Range(1, 12)] private int m_waveLobes = 5;

    [Tooltip("How fast the wave pattern rotates around the rings, in radians per second (unscaled).")]
    [SerializeField] private float m_waveSpin = 6f;

    [Header("Lifecycle")]
    [Tooltip("Fraction of a ring's life it stays fully opaque before it starts fading (0..1).")]
    [SerializeField, Range(0f, 0.95f)] private float m_holdFraction = 0.25f;

    [Header("Look")]
    [Tooltip("Base colour if Play() isn't given one. Alpha here is the opaque value the fade scales from.")]
    [SerializeField] private Color m_color = new Color(1f, 0.55f, 0.15f, 0.9f);

    [Tooltip("RGB multiplier so the rings blow past 1.0 and the bloom volume catches them.")]
    [SerializeField, Min(1f)] private float m_intensity = 2.4f;

    [Tooltip("Material for the ring LineRenderers — the same unlit sprite/line material the other procedural FX use.")]
    [SerializeField] private Material m_lineMaterial;

    [Tooltip("Sorting layer ID for the rings (0 = Default).")]
    [SerializeField] private int m_sortingLayerId;

    [Tooltip("Sorting order within the layer.")]
    [SerializeField] private int m_sortingOrder = 100;

    private ObjectPool m_pool;
    private LineRenderer[] m_rings;
    private float[] m_ringPhase;     // per-ring starting wobble phase, so they don't move in lockstep
    private float m_radius;
    private Color m_rgb;
    private float m_alphaBasis;
    private float m_age;             // unscaled seconds since the last Play()
    private float m_totalLifetime;

    private void Awake()
    {
        int count = Mathf.Max(1, m_rippleCount);
        int verts = Mathf.Max(3, m_segments);
        m_rings = new LineRenderer[count];
        m_ringPhase = new float[count];

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject($"Ripple{i}");
            go.transform.SetParent(transform, false);

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;            // ring is centred on this GameObject's position
            lr.loop = true;
            lr.numCornerVertices = 0;
            lr.numCapVertices = 0;
            lr.alignment = LineAlignment.View;
            lr.textureMode = LineTextureMode.Stretch;
            lr.positionCount = verts;
            lr.sortingLayerID = m_sortingLayerId;
            lr.sortingOrder = m_sortingOrder;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            lr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            if (m_lineMaterial != null) lr.sharedMaterial = m_lineMaterial;
            lr.enabled = false;

            m_rings[i] = lr;
            m_ringPhase[i] = i * (Mathf.PI * 2f / count) + i * 0.7f;
        }

        m_radius = m_fallbackRadius;
        m_rgb = m_color;
        m_alphaBasis = m_color.a;
        m_totalLifetime = (count - 1) * m_rippleStagger + m_rippleLifetime;
    }

    private void OnEnable()
    {
        if (m_pool == null) ServiceLocator.Global.TryGet(out m_pool);
        if (m_rings == null) return;
        foreach (LineRenderer lr in m_rings)
            if (lr != null) lr.enabled = false;
    }

    /// <summary>
    /// Size, colour, and (re)start the ripple cascade. Call right after pulling the prefab from
    /// the pool — it idles invisible until then. A <paramref name="radius"/> &lt;= 0 keeps the fallback.
    /// </summary>
    public void Play(float radius, Color color)
    {
        m_radius = radius > 0f ? radius : m_fallbackRadius;
        m_rgb = color;
        m_alphaBasis = color.a > 0f ? color.a : m_color.a;
        m_age = 0f;
        SampleAll();

        StopAllCoroutines();
        if (m_pool != null)
            StartCoroutine(m_pool.ReturnAfter(gameObject, m_totalLifetime));
    }

    private void Update()
    {
        m_age += Time.unscaledDeltaTime;
        SampleAll();
    }

    private void SampleAll()
    {
        if (m_rings == null) return;
        for (int i = 0; i < m_rings.Length; i++)
            SampleRing(i);
    }

    private void SampleRing(int i)
    {
        LineRenderer lr = m_rings[i];
        if (lr == null) return;

        float age = m_age - i * m_rippleStagger;
        if (age < 0f || age > m_rippleLifetime)
        {
            if (lr.enabled) lr.enabled = false;
            return;
        }
        if (!lr.enabled) lr.enabled = true;

        float t = Mathf.Clamp01(age / m_rippleLifetime);

        // Ease-out expansion so it reads as a snap, not a drift.
        float baseRadius = Mathf.Lerp(m_startRadius, m_radius, Mathf.Sqrt(t));

        // Echoes are thinner; the wobble shrinks as the ring expands and dissipates.
        float echo = m_rings.Length > 1 ? Mathf.Lerp(1f, m_echoWidthFalloff, i / (float)(m_rings.Length - 1)) : 1f;
        float width = m_startWidth * echo * (1f - t);
        float wobble = m_waveAmplitude * (1f - t);

        // Hold opaque for a bit, then linear fade to zero.
        float fade = Mathf.Clamp01((1f - t) / Mathf.Max(0.01f, 1f - m_holdFraction));
        float alpha = m_alphaBasis * fade;

        float phase = m_ringPhase[i] + m_waveSpin * m_age;
        int n = lr.positionCount;
        for (int v = 0; v < n; v++)
        {
            float a = (v / (float)n) * Mathf.PI * 2f;
            float r = baseRadius + wobble * Mathf.Sin(a * m_waveLobes + phase);
            lr.SetPosition(v, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f));
        }
        lr.startWidth = width;
        lr.endWidth = width;
        Color c = new Color(m_rgb.r * m_intensity, m_rgb.g * m_intensity, m_rgb.b * m_intensity, alpha);
        lr.startColor = c;
        lr.endColor = c;
    }
}
