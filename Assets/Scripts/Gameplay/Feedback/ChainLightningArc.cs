using Gameplay.ObjectPool;
using UnityEngine;
using UnityServiceLocator;

/// <summary>
/// Pooled visual for a single chain-lightning hop. Draws a procedural zigzag between two
/// world points using a <see cref="LineRenderer"/>, periodically re-jitters its midpoints
/// so the arc crackles, then fades width + alpha and returns itself to the pool.
/// <para>
/// Pixel-art read: keep the segment count low (chunky elbows), keep the width a small
/// whole number of pixels at the project PPU, and let the URP bloom volume do the glow.
/// </para>
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class ChainLightningArc : MonoBehaviour
{
    [SerializeField] private LineRenderer m_line;

    [Header("Shape")]
    [Tooltip("Number of straight segments in the zigzag. Lower = chunkier silhouette.")]
    [SerializeField, Range(2, 32)] private int m_segments = 8;

    [Tooltip("Maximum perpendicular displacement of interior segment vertices, in world units.")]
    [SerializeField, Min(0f)] private float m_jitterAmplitude = 0.25f;

    [Header("Lifecycle")]
    [Tooltip("Total seconds the arc is visible before it returns to the pool.")]
    [SerializeField, Min(0.01f)] private float m_lifetime = 0.16f;

    [Tooltip("Seconds between zigzag re-rolls. Smaller = faster crackle.")]
    [SerializeField, Min(0.01f)] private float m_jitterInterval = 0.04f;

    [Header("Look")]
    [Tooltip("Color (with alpha) sampled across the arc's lifetime — left = spawn, right = despawn.")]
    [SerializeField] private Gradient m_colorOverLifetime;

    [Tooltip("Width multiplier sampled across the arc's lifetime.")]
    [SerializeField] private AnimationCurve m_widthOverLifetime = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    [Tooltip("Base width of the line, in world units. Pick a small whole number of pixels at your PPU.")]
    [SerializeField, Min(0f)] private float m_baseWidth = 0.12f;

    private ObjectPool m_pool;
    private Vector3 m_start;
    private Vector3 m_end;
    private float m_age;
    private float m_nextJitterAt;
    private GradientColorKey[] m_baseColorKeys;

    private void Reset()
    {
        m_line = GetComponent<LineRenderer>();
    }

    private void Awake()
    {
        if (m_line == null)
            m_line = GetComponent<LineRenderer>();

        // Force world-space positions so the prefab's transform doesn't drag the arc around.
        m_line.useWorldSpace = true;
        m_line.numCornerVertices = 0;
        m_line.numCapVertices = 0;
        m_line.alignment = LineAlignment.View;
        m_line.textureMode = LineTextureMode.Tile;

        // Snapshot the authored gradient stops so SetTint can multiply against them rather
        // than overwriting — otherwise the first SetTint loses the prefab's color shape and
        // every subsequent call keeps stacking onto the already-tinted gradient.
        GradientColorKey[] authored = m_colorOverLifetime.colorKeys;
        m_baseColorKeys = new GradientColorKey[authored.Length];
        for (int i = 0; i < authored.Length; i++)
            m_baseColorKeys[i] = authored[i];
    }

    private void OnEnable()
    {
        if (m_pool == null)
            ServiceLocator.Global.TryGet(out m_pool);
    }

    /// <summary>
    /// Set the arc's endpoints and (re)start its visual lifecycle. Call immediately after
    /// pulling the prefab from the pool — the arc otherwise idles at zero length.
    /// </summary>
    public void Play(Vector3 start, Vector3 end)
    {
        m_start = start;
        m_end = end;
        m_age = 0f;
        m_nextJitterAt = 0f;
        Rebuild();
        ApplyLifetimeSamples(0f);

        // Toggle the renderer so Unity invalidates the cached world-space bounds left behind
        // by this instance's previous spawn — otherwise frustum culling can drop the first
        // render frame when the new endpoints differ significantly from the old ones.
        m_line.enabled = false;
        m_line.enabled = true;

        // Drive lifetime/return-to-pool from Play rather than OnEnable so a pooled instance
        // that's reused before its previous coroutine completed gets a clean lifecycle.
        StopAllCoroutines();
        if (m_pool != null)
            StartCoroutine(m_pool.ReturnAfter(gameObject, m_lifetime));
    }

    public void SetTint(Color color)
    {
        // Multiply the authored gradient stops by the tint so callers can recolor the bolt
        // without flattening the prefab's color shape. White tint = original gradient;
        // any other tint scales each channel of every stop.
        GradientColorKey[] tinted = new GradientColorKey[m_baseColorKeys.Length];
        for (int i = 0; i < m_baseColorKeys.Length; i++)
        {
            Color basis = m_baseColorKeys[i].color;
            tinted[i] = new GradientColorKey(
                new Color(basis.r * color.r, basis.g * color.g, basis.b * color.b, basis.a * color.a),
                m_baseColorKeys[i].time);
        }
        m_colorOverLifetime.SetKeys(tinted, m_colorOverLifetime.alphaKeys);
    }

    private void Update()
    {
        m_age += Time.deltaTime;
        float t = Mathf.Clamp01(m_age / m_lifetime);

        if (m_age >= m_nextJitterAt)
        {
            m_nextJitterAt += m_jitterInterval;
            Rebuild();
        }

        ApplyLifetimeSamples(t);
    }

    private void ApplyLifetimeSamples(float t)
    {
        Color c = m_colorOverLifetime.Evaluate(t);
        m_line.startColor = c;
        m_line.endColor = c;

        float w = m_baseWidth * m_widthOverLifetime.Evaluate(t);
        m_line.startWidth = w;
        m_line.endWidth = w;
    }

    private void Rebuild()
    {
        Vector3 dir = m_end - m_start;
        float length = dir.magnitude;
        if (length < 0.0001f)
        {
            m_line.positionCount = 2;
            m_line.SetPosition(0, m_start);
            m_line.SetPosition(1, m_end);
            return;
        }

        Vector3 dirN = dir / length;
        Vector3 perp = new Vector3(-dirN.y, dirN.x, 0f);

        m_line.positionCount = m_segments + 1;
        for (int i = 0; i <= m_segments; i++)
        {
            float u = (float)i / m_segments;
            Vector3 basePoint = m_start + dir * u;
            // Pin the endpoints so the arc actually connects; jitter only the interior.
            float jitter = (i == 0 || i == m_segments)
                ? 0f
                : (Random.value - 0.5f) * 2f * m_jitterAmplitude;
            m_line.SetPosition(i, basePoint + perp * jitter);
        }
    }
}
