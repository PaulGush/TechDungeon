using UnityEngine;
using UnityServiceLocator;

/// <summary>
/// Energy-bubble visual for the Phase Shield ability. Lives as a persistent child of the player
/// rig (so it tracks the player with zero follow code) and is normally hidden. Listens for
/// <see cref="PlayerStatusEffects.BuffKind.Invulnerable"/> starting/ending and pops a procedural
/// <see cref="LineRenderer"/> ring in and out, crackling and breathing gently while it's up.
/// <para>
/// The ring is rebuilt every frame from ~16 vertices — cheap, and lets us animate radius, spin,
/// and per-vertex shimmer without touching the transform (so the pop-in scale never drags the
/// player's other children around). It's state-driven, not duration-driven: a re-cast that just
/// refreshes the buff re-pulses the ring, and the ring dissipates the instant the buff actually
/// ends, however that happens.
/// </para>
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class ShieldVfx : MonoBehaviour
{
    [SerializeField] private LineRenderer m_line;

    [Header("Shape")]
    [Tooltip("Vertex count of the ring. ~12-20 reads as a smooth bubble; lower looks faceted/sci-fi.")]
    [SerializeField, Range(6, 48)] private int m_segments = 16;

    [Tooltip("Resting radius of the bubble, in world units. ~0.65 wraps the player capsule. " +
             "Keep ShieldDeflector's collider radius in sync so shots stop where the ring is drawn.")]
    [SerializeField, Min(0.05f)] private float m_radius = 0.65f;

    [Tooltip("Line width in world units. A small whole number of pixels at the project PPU reads cleanest.")]
    [SerializeField, Min(0f)] private float m_width = 0.06f;

    [Header("Idle motion")]
    [Tooltip("Degrees per second the ring slowly rotates while active.")]
    [SerializeField] private float m_spinSpeed = 35f;

    [Tooltip("Peak in/out radius wobble (breathing) while active, in world units.")]
    [SerializeField, Min(0f)] private float m_breatheAmplitude = 0.03f;

    [Tooltip("Breaths per second.")]
    [SerializeField, Min(0f)] private float m_breatheFrequency = 1.4f;

    [Tooltip("Peak random per-vertex radial shimmer, in world units.")]
    [SerializeField, Min(0f)] private float m_shimmerAmplitude = 0.02f;

    [Tooltip("Seconds between shimmer re-rolls. Smaller = faster crackle.")]
    [SerializeField, Min(0.01f)] private float m_shimmerInterval = 0.05f;

    [Header("Lifecycle")]
    [Tooltip("Seconds for the bubble to pop in (snaps out past full size, then settles).")]
    [SerializeField, Min(0.01f)] private float m_popInSeconds = 0.18f;

    [Tooltip("How far past the resting radius the pop-in overshoots (0.25 = +25%).")]
    [SerializeField, Range(0f, 1f)] private float m_popOvershoot = 0.25f;

    [Tooltip("Seconds for the bubble to dissipate when the shield ends (expands while fading).")]
    [SerializeField, Min(0.01f)] private float m_fadeOutSeconds = 0.3f;

    [Tooltip("How far the bubble expands while dissipating (0.3 = +30%).")]
    [SerializeField, Range(0f, 1f)] private float m_fadeExpand = 0.3f;

    [Header("Look")]
    [Tooltip("Bubble colour. Push channels past 1 so URP bloom catches it. Alpha is the steady value held while active.")]
    [SerializeField] private Color m_color = new Color(0.5f, 1.7f, 1.2f, 0.8f);

    private enum Phase { Hidden, PopIn, Sustain, FadeOut }
    private Phase m_phase = Phase.Hidden;

    private PlayerStatusEffects m_status;
    private float m_phaseTime;       // seconds elapsed in the current phase
    private float m_spinRadians;     // accumulated rotation
    private float m_nextShimmerAt;   // phase time of the next shimmer re-roll
    private float[] m_shimmer;       // per-vertex radial offset

    private void Reset() => m_line = GetComponent<LineRenderer>();

    private void Awake()
    {
        if (m_line == null) m_line = GetComponent<LineRenderer>();

        m_line.useWorldSpace = false;            // child of the player rig → follows it for free
        m_line.loop = true;
        m_line.numCornerVertices = 0;
        m_line.numCapVertices = 0;
        m_line.alignment = LineAlignment.View;
        m_line.textureMode = LineTextureMode.Stretch;
        m_line.positionCount = Mathf.Max(3, m_segments);
        m_shimmer = new float[m_line.positionCount];

        m_line.enabled = false;
    }

    private void Start()
    {
        // PlayerStatusEffects registers itself globally in its Awake; safe to grab from Start.
        if (!ServiceLocator.Global.TryGet(out m_status)) return;

        m_status.OnBuffStarted += OnBuffStarted;
        m_status.OnBuffEnded += OnBuffEnded;

        // Defensive: if the shield was already up before we hooked in, show it immediately.
        if (m_status.IsActive(PlayerStatusEffects.BuffKind.Invulnerable))
            BeginPopIn();
    }

    private void OnDestroy()
    {
        if (m_status == null) return;
        m_status.OnBuffStarted -= OnBuffStarted;
        m_status.OnBuffEnded -= OnBuffEnded;
    }

    private void OnBuffStarted(PlayerStatusEffects.BuffKind kind, float _)
    {
        if (kind == PlayerStatusEffects.BuffKind.Invulnerable)
            BeginPopIn();   // fresh cast or a re-cast that refreshed the buff — re-pop either way
    }

    private void OnBuffEnded(PlayerStatusEffects.BuffKind kind)
    {
        if (kind != PlayerStatusEffects.BuffKind.Invulnerable || m_phase == Phase.Hidden) return;
        m_phase = Phase.FadeOut;
        m_phaseTime = 0f;
    }

    private void BeginPopIn()
    {
        if (m_phase == Phase.Hidden) m_spinRadians = 0f;
        m_phase = Phase.PopIn;
        m_phaseTime = 0f;
        m_nextShimmerAt = 0f;
        m_line.enabled = true;
    }

    private void Update()
    {
        if (m_phase == Phase.Hidden) return;

        float dt = Time.deltaTime;
        m_phaseTime += dt;
        m_spinRadians += m_spinSpeed * Mathf.Deg2Rad * dt;

        if (m_phaseTime >= m_nextShimmerAt)
        {
            m_nextShimmerAt += m_shimmerInterval;
            RollShimmer();
        }

        float radius, width, alpha;
        switch (m_phase)
        {
            case Phase.PopIn:
            {
                float t = Mathf.Clamp01(m_phaseTime / m_popInSeconds);
                radius = m_radius * PopCurve(t, m_popOvershoot);
                width = m_width * Mathf.Lerp(1.7f, 1f, t);   // thick on the snap, thins as it settles
                alpha = m_color.a * Mathf.Clamp01(t * 2.5f);
                if (t >= 1f) { m_phase = Phase.Sustain; m_phaseTime = 0f; }
                break;
            }
            case Phase.Sustain:
            {
                radius = m_radius + Mathf.Sin(m_phaseTime * m_breatheFrequency * Mathf.PI * 2f) * m_breatheAmplitude;
                width = m_width;
                alpha = m_color.a;
                break;
            }
            default: // FadeOut
            {
                float t = Mathf.Clamp01(m_phaseTime / m_fadeOutSeconds);
                radius = m_radius * (1f + m_fadeExpand * t);
                width = m_width * (1f - 0.5f * t);
                alpha = m_color.a * (1f - t * t);            // ease-in fade
                if (t >= 1f) { m_phase = Phase.Hidden; m_line.enabled = false; return; }
                break;
            }
        }

        RebuildRing(radius, width, alpha);
    }

    private void RollShimmer()
    {
        for (int i = 0; i < m_shimmer.Length; i++)
            m_shimmer[i] = (Random.value - 0.5f) * 2f * m_shimmerAmplitude;
    }

    private void RebuildRing(float radius, float width, float alpha)
    {
        int n = m_line.positionCount;
        for (int i = 0; i < n; i++)
        {
            float a = m_spinRadians + (i / (float)n) * Mathf.PI * 2f;
            float r = radius + m_shimmer[i];
            m_line.SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f));
        }
        m_line.startWidth = width;
        m_line.endWidth = width;
        Color c = m_color;
        c.a = alpha;
        m_line.startColor = c;
        m_line.endColor = c;
    }

    // Snap out past the resting radius over the first 60% of the pop, then ease back to 1.
    // overshoot=0 collapses to a plain ease-in/settle. Predictable and cheap.
    private static float PopCurve(float t, float overshoot)
    {
        if (t < 0.6f)
            return Mathf.SmoothStep(0f, 1f + overshoot, t / 0.6f);
        return Mathf.Lerp(1f + overshoot, 1f, Mathf.SmoothStep(0f, 1f, (t - 0.6f) / 0.4f));
    }
}
