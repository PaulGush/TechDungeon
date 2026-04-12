using System;
using System.Collections;
using Gameplay.ObjectPool;
using UnityEngine;
using UnityServiceLocator;

public class SpawnIndicator : MonoBehaviour
{
    [SerializeField] private SpriteRenderer m_spriteRenderer;
    [SerializeField] private Color m_color = new(1f, 0.2f, 0.2f, 0.5f);
    [SerializeField] private int m_sortingOrder = 2;

    private ObjectPool m_pool;
    private Coroutine m_animateCoroutine;
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

        m_spriteRenderer.sprite = s_circleSprite;
        m_spriteRenderer.sortingOrder = m_sortingOrder;
    }

    private void OnDisable()
    {
        if (m_animateCoroutine != null)
        {
            StopCoroutine(m_animateCoroutine);
            m_animateCoroutine = null;
        }
    }

    public void Play(float duration)
    {
        m_spriteRenderer.color = m_color;
        m_animateCoroutine = StartCoroutine(Animate(duration));
    }

    private IEnumerator Animate(float duration)
    {
        float elapsed = 0f;
        transform.localScale = Vector3.zero;

        Color baseColor = m_color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float scale = 1f - (1f - t) * (1f - t);
            transform.localScale = new Vector3(scale, scale, 1f);

            baseColor.a = Mathf.Lerp(0.3f, 0.6f, Mathf.PingPong(t * 6f, 1f));
            m_spriteRenderer.color = baseColor;

            yield return null;
        }

        m_animateCoroutine = null;

        if (m_pool == null || !m_pool.ReturnGameObject(gameObject))
            Destroy(gameObject);
    }

    private static Sprite CreateCircleSprite()
    {
        const int size = 64;
        const float radius = size / 2f;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

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
