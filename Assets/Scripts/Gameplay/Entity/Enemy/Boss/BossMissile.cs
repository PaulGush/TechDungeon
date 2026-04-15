using Gameplay.ObjectPool;
using UnityEngine;
using UnityServiceLocator;

/// <summary>
/// A single missile launched by <see cref="MissileBarrage"/>. Travels from a launch point
/// to a fixed ground-target along a parabolic arc, then spawns an ExplosionEffect at the
/// impact site which handles the trigger-based area damage and falloff itself.
/// Self-contained: generates its own sprite at first enable so no manual prefab authoring is required.
/// </summary>
public class BossMissile : MonoBehaviour
{
    private const int SpriteSize = 16;
    private const int SortingOrder = 5;

    private static Sprite s_missileSprite;

    [SerializeField] private SpriteRenderer m_spriteRenderer;
    [SerializeField] private Color m_color = new Color(1f, 0.55f, 0.15f, 1f);
    [SerializeField] private float m_baseScale = 0.4f;

    private ObjectPool m_pool;
    private Vector2 m_startPosition;
    private Vector2 m_targetPosition;
    private float m_travelDuration;
    private float m_arcHeight;
    private float m_elapsed;
    private int m_damage;
    private float m_explosionRadius;
    private LayerMask m_damageLayers;
    private GameObject m_explosionEffectPrefab;
    private bool m_active;

    private void Awake()
    {
        ServiceLocator.Global.TryGet(out m_pool);

        if (m_spriteRenderer == null)
        {
            m_spriteRenderer = GetComponent<SpriteRenderer>();
            if (m_spriteRenderer == null)
                m_spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (s_missileSprite == null)
            s_missileSprite = CreateMissileSprite();

        m_spriteRenderer.sprite = s_missileSprite;
        m_spriteRenderer.color = m_color;
        m_spriteRenderer.sortingOrder = SortingOrder;
    }

    public void Launch(
        Vector2 startPosition,
        Vector2 targetPosition,
        float travelDuration,
        float arcHeightRatio,
        int damage,
        float explosionRadius,
        LayerMask damageLayers,
        GameObject explosionEffectPrefab)
    {
        m_startPosition = startPosition;
        m_targetPosition = targetPosition;
        m_travelDuration = Mathf.Max(travelDuration, 0.01f);
        m_arcHeight = Vector2.Distance(startPosition, targetPosition) * arcHeightRatio;
        m_elapsed = 0f;
        m_damage = damage;
        m_explosionRadius = explosionRadius;
        m_damageLayers = damageLayers;
        m_explosionEffectPrefab = explosionEffectPrefab;
        m_active = true;

        transform.position = startPosition;
        transform.localScale = Vector3.one * m_baseScale;
    }

    private void Update()
    {
        if (!m_active) return;

        m_elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(m_elapsed / m_travelDuration);

        Vector2 ground = Vector2.Lerp(m_startPosition, m_targetPosition, t);
        // Parabolic arc — sin(πt) peaks at t=0.5, returns to 0 at t=1.
        float height = Mathf.Sin(t * Mathf.PI) * m_arcHeight;
        Vector3 position = new Vector3(ground.x, ground.y + height, transform.position.z);

        // Aim the sprite along its instantaneous travel direction so it visually nose-dives at impact.
        Vector3 forward = position - transform.position;
        if (forward.sqrMagnitude > Mathf.Epsilon)
            transform.rotation = MathUtilities.CalculateAimRotation(forward, -90f);

        transform.position = position;

        if (t >= 1f)
            Impact();
    }

    private void Impact()
    {
        m_active = false;

        if (m_explosionEffectPrefab != null)
        {
            GameObject vfx = m_pool != null
                ? m_pool.GetPooledObject(m_explosionEffectPrefab)
                : Instantiate(m_explosionEffectPrefab);
            vfx.transform.position = m_targetPosition;
            vfx.transform.rotation = Quaternion.identity;

            if (vfx.TryGetComponent(out ExplosionEffect explosion))
                explosion.Initialize(m_explosionRadius, m_damage, m_damageLayers);
        }

        if (m_pool == null || !m_pool.ReturnGameObject(gameObject))
            Destroy(gameObject);
    }

    private static Sprite CreateMissileSprite()
    {
        Texture2D tex = new Texture2D(SpriteSize, SpriteSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point
        };

        Color[] pixels = new Color[SpriteSize * SpriteSize];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        // Draw a simple vertical missile silhouette (body + nose) pointed up.
        const int bodyHalfWidth = 2;
        for (int y = 2; y < SpriteSize - 4; y++)
        {
            for (int x = SpriteSize / 2 - bodyHalfWidth; x <= SpriteSize / 2 + bodyHalfWidth - 1; x++)
                pixels[y * SpriteSize + x] = Color.white;
        }
        // Nose cone — narrowing triangle.
        for (int y = SpriteSize - 4; y < SpriteSize - 1; y++)
        {
            int width = bodyHalfWidth - (y - (SpriteSize - 4));
            for (int x = SpriteSize / 2 - width; x <= SpriteSize / 2 + width - 1; x++)
                pixels[y * SpriteSize + x] = Color.white;
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, SpriteSize, SpriteSize), new Vector2(0.5f, 0.5f), SpriteSize);
    }
}
