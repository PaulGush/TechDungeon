using System.Collections;
using Gameplay.ObjectPool;
using UnityEngine;
using UnityServiceLocator;

public class TeleportPuff : MonoBehaviour
{
    [SerializeField] private SpriteRenderer m_spriteRenderer;
    [SerializeField] private int m_sortingOrder = 3;

    [Header("Animation")]
    [Tooltip("Seconds the puff takes to expand from zero to its full scale and fade out.")]
    [SerializeField] private float m_duration = 0.25f;

    [Tooltip("Target world-space scale at the peak of the expansion.")]
    [SerializeField] private float m_peakScale = 1.25f;

    [Tooltip("Tint applied to the generated puff sprite. Alpha drives the starting opacity.")]
    [SerializeField] private Color m_color = new(0.7f, 0.85f, 1f, 0.75f);

    private ObjectPool m_pool;
    private Coroutine m_routine;
    private static Sprite s_circleSprite;

    private void Awake()
    {
        ServiceLocator.Global.TryGet(out m_pool);

        if (m_spriteRenderer == null)
        {
            m_spriteRenderer = GetComponent<SpriteRenderer>();
            if (m_spriteRenderer == null)
                m_spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (s_circleSprite == null)
            s_circleSprite = CreateCircleSprite();

        if (m_spriteRenderer.sprite == null)
            m_spriteRenderer.sprite = s_circleSprite;
        m_spriteRenderer.sortingOrder = m_sortingOrder;
    }

    private void OnEnable()
    {
        m_routine = StartCoroutine(Animate());
    }

    private void OnDisable()
    {
        if (m_routine != null)
        {
            StopCoroutine(m_routine);
            m_routine = null;
        }
    }

    private IEnumerator Animate()
    {
        float elapsed = 0f;
        transform.localScale = Vector3.zero;
        m_spriteRenderer.color = m_color;

        Color c = m_color;
        float startAlpha = m_color.a;

        while (elapsed < m_duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / m_duration);

            // Ease-out expansion, linear alpha fade to zero.
            float scale = (1f - (1f - t) * (1f - t)) * m_peakScale;
            transform.localScale = new Vector3(scale, scale, 1f);

            c.a = Mathf.Lerp(startAlpha, 0f, t);
            m_spriteRenderer.color = c;

            yield return null;
        }

        m_routine = null;

        if (m_pool == null || !m_pool.ReturnGameObject(gameObject))
            Destroy(gameObject);
    }

    private static Sprite CreateCircleSprite()
    {
        const int size = 64;
        const float radius = size / 2f;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
                tex.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
