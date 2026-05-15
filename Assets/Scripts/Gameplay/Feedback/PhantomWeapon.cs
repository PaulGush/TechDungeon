using Gameplay.ObjectPool;
using UnityEngine;
using UnityServiceLocator;

/// <summary>
/// A single phantom weapon spawned by the Projectile Burst ability. A pooled SpriteRenderer
/// that takes on the look of whatever weapon the player is holding, fades in, fires one shot
/// outward at its scheduled moment, then fades back out and returns itself to the pool. The
/// ability parents it under the player transform so the radial formation follows the player
/// if they move during the cast.
/// <para>
/// Uses unscaled time so the burst still reads cleanly through any hit-stop a phantom's shot
/// might kick off when it lands.
/// </para>
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PhantomWeapon : MonoBehaviour
{
    [SerializeField] private SpriteRenderer m_renderer;

    [Header("Timing (unscaled seconds)")]
    [Tooltip("Time the phantom takes to fade up to full alpha after spawning.")]
    [SerializeField, Min(0f)] private float m_fadeIn = 0.06f;

    [Tooltip("Time the phantom holds visible before firing its single shot.")]
    [SerializeField, Min(0f)] private float m_hold = 0.08f;

    [Tooltip("Time after the shot before the fade-out starts — lets the muzzle-flash colour punch read.")]
    [SerializeField, Min(0f)] private float m_postFire = 0.05f;

    [Tooltip("Time the phantom takes to fade out before returning to the pool.")]
    [SerializeField, Min(0.01f)] private float m_fadeOut = 0.18f;

    [Header("Look")]
    [Tooltip("Peak alpha while the phantom is fully visible. Below 1 reads as ghosted.")]
    [SerializeField, Range(0f, 1f)] private float m_peakAlpha = 0.85f;

    [Tooltip("Colour to flash to at the moment of firing — push channels past 1 for a bright bloom punch. Eases back to the phantom's tint over PostFire.")]
    [SerializeField] private Color m_fireFlash = new Color(2.4f, 2.4f, 2.4f, 1f);

    /// <summary>Total unscaled seconds from <see cref="Play"/> to pool return for a single-shot phantom.</summary>
    public float TotalLifetime => m_fadeIn + m_hold + m_postFire + m_fadeOut;

    /// <summary>Total lifetime when the phantom fires a burst. The post-fire window stretches to
    /// at least the burst duration so all shots actually land before the phantom fades out.</summary>
    public float ComputeBurstLifetime(int burstCount, float burstInterval)
    {
        float burstDuration = burstCount > 1 ? (burstCount - 1) * Mathf.Max(0f, burstInterval) : 0f;
        return m_fadeIn + m_hold + Mathf.Max(m_postFire, burstDuration) + m_fadeOut;
    }

    private ObjectPool m_pool;
    private GameObject m_projectilePrefab;
    private AmmoSettings m_intrinsicAmmo;   // the weapon's built-in round (e.g. RPG missile) — always layered in
    private AmmoSettings m_loadedAmmo;      // the special ammo the player has loaded, or null
    private int m_bonusDamage;
    private int m_bonusPierce;
    private int m_pelletsPerShot = 1;       // matches the source weapon's PelletsPerShot
    private float m_spreadDegrees;          // matches the source weapon's authored SpreadDegrees
    private int m_burstCount = 1;           // 1 = single shot; >1 = burst-fire weapon (e.g. M16)
    private float m_burstInterval;          // gap between shots in a burst, unscaled seconds
    private int m_shotsFired;
    private float m_nextShotTime;
    private Color m_tint = Color.white;
    private bool m_playStarted;
    private float m_age;

    private void Reset() => m_renderer = GetComponent<SpriteRenderer>();

    private void Awake()
    {
        if (m_renderer == null) m_renderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (m_pool == null) ServiceLocator.Global.TryGet(out m_pool);

        // Reset bookkeeping on every (re)spawn so an Update that fires before Play has had a
        // chance to run can't accidentally read stale age from the previous use.
        m_playStarted = false;
        m_shotsFired = 0;
        m_age = 0f;
        if (m_renderer != null)
        {
            Color c = m_renderer.color;
            c.a = 0f;
            m_renderer.color = c;
        }
    }

    /// <summary>
    /// Configure and start the phantom. The phantom is reparented under <paramref name="parent"/>
    /// at <paramref name="localOffset"/> with the given Z rotation and Y flip (mirror of the
    /// held weapon's auto-flip rule), takes on <paramref name="sprite"/> as its visual, and will
    /// fire a single <paramref name="projectilePrefab"/> shot outward when its timeline reaches
    /// the fire moment — layering the weapon's <paramref name="intrinsicAmmo"/> behaviour with
    /// any <paramref name="loadedAmmo"/> the player has, so e.g. an RPG burst with ricochet ammo
    /// loaded fires bouncing missiles that still explode.
    /// </summary>
    public void Play(
        Transform parent,
        Vector3 localOffset,
        float localRotZ,
        Sprite sprite,
        Color tint,
        bool flipY,
        int sortingLayerId,
        int sortingOrder,
        GameObject projectilePrefab,
        AmmoSettings intrinsicAmmo,
        AmmoSettings loadedAmmo,
        int bonusDamage,
        int bonusPierce,
        int pelletsPerShot,
        float spreadDegrees,
        int burstCount,
        float burstInterval)
    {
        transform.SetParent(parent, worldPositionStays: false);
        transform.localPosition = localOffset;
        transform.localRotation = Quaternion.Euler(0f, 0f, localRotZ);
        transform.localScale = new Vector3(1f, flipY ? -1f : 1f, 1f);

        m_renderer.sprite = sprite;
        m_renderer.sortingLayerID = sortingLayerId;
        m_renderer.sortingOrder = sortingOrder;
        Color c = tint;
        c.a = 0f;
        m_renderer.color = c;

        m_tint = tint;
        m_projectilePrefab = projectilePrefab;
        m_intrinsicAmmo = intrinsicAmmo;
        m_loadedAmmo = loadedAmmo;
        m_bonusDamage = bonusDamage;
        m_bonusPierce = bonusPierce;
        m_pelletsPerShot = Mathf.Max(1, pelletsPerShot);
        m_spreadDegrees = Mathf.Max(0f, spreadDegrees);
        m_burstCount = Mathf.Max(1, burstCount);
        m_burstInterval = Mathf.Max(0f, burstInterval);
        m_shotsFired = 0;
        m_nextShotTime = m_fadeIn + m_hold;
        m_age = 0f;
        m_playStarted = true;
    }

    private void Update()
    {
        if (!m_playStarted) return;

        m_age += Time.unscaledDeltaTime;

        float fireMoment = m_fadeIn + m_hold;
        float burstDuration = m_burstCount > 1 ? (m_burstCount - 1) * m_burstInterval : 0f;
        // Stretch the post-fire window to cover the entire burst so all shots land before fadeOut.
        float postFireWindow = Mathf.Max(m_postFire, burstDuration);
        float postFireEnd = fireMoment + postFireWindow;
        float total = postFireEnd + m_fadeOut;

        Color c;
        if (m_age < m_fadeIn)
        {
            float t = m_age / Mathf.Max(0.0001f, m_fadeIn);
            c = m_tint;
            c.a = m_peakAlpha * t;
        }
        else if (m_age < fireMoment)
        {
            c = m_tint;
            c.a = m_peakAlpha;
        }
        else if (m_age < postFireEnd)
        {
            // Ease from the bright fire flash back down to the steady tint over postFireWindow,
            // so burst weapons hold the bright punch for the whole burst rather than instantly.
            float t = (m_age - fireMoment) / Mathf.Max(0.0001f, postFireWindow);
            c = Color.Lerp(m_fireFlash, m_tint, t);
            c.a = m_peakAlpha;
        }
        else
        {
            float t = Mathf.Clamp01((m_age - postFireEnd) / Mathf.Max(0.0001f, m_fadeOut));
            c = m_tint;
            c.a = m_peakAlpha * (1f - t);
        }
        m_renderer.color = c;

        // Burst-aware firing: keep firing on schedule until all rounds in the burst are out. For
        // non-burst weapons m_burstCount is 1 and this fires exactly once at fireMoment.
        while (m_shotsFired < m_burstCount && m_age >= m_nextShotTime)
        {
            m_shotsFired++;
            m_nextShotTime += m_burstInterval;
            FireShot();
        }

        if (m_age >= total)
        {
            m_playStarted = false;
            if (m_pool != null && !m_pool.ReturnGameObject(gameObject))
                gameObject.SetActive(false);
        }
    }

    private void FireShot()
    {
        if (m_projectilePrefab == null || m_pool == null) return;

        // The phantom inherits the held-weapon convention: its world Z rotation equals the aim
        // angle, so transform.right points outward. Derive the fire rotation from transform.right
        // so any parent rotation (the player transform along the way) is respected automatically.
        Vector2 worldDir = transform.right;
        float baseAngle = Mathf.Atan2(worldDir.y, worldDir.x) * Mathf.Rad2Deg;

        AmmoSettings displayAmmo = m_loadedAmmo != null ? m_loadedAmmo : m_intrinsicAmmo;

        // One projectile per pellet so multi-pellet weapons (shotgun) read as a real shotgun
        // blast per phantom rather than a single round. Each pellet gets its own random spread
        // and its own ammo effect — some effects (ricochet) carry per-shot state.
        for (int i = 0; i < m_pelletsPerShot; i++)
        {
            float angle = baseAngle;
            if (m_spreadDegrees > 0f)
                angle += UnityEngine.Random.Range(-m_spreadDegrees, m_spreadDegrees);
            Quaternion rot = Quaternion.Euler(0f, 0f, angle);

            IAmmoEffect effect = CompositeAmmoEffect.Compose(
                m_intrinsicAmmo != null ? m_intrinsicAmmo.CreateEffect() : null,
                m_loadedAmmo != null ? m_loadedAmmo.CreateEffect() : null);

            ProjectileSpawner.Spawn(
                m_pool, m_projectilePrefab, transform.position, rot,
                bonusDamage: m_bonusDamage,
                bonusPierce: m_bonusPierce,
                ammoSettings: displayAmmo,
                ammoEffect: effect);
        }
    }
}
