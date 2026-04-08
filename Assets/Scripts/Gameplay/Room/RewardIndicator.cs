using System.Collections.Generic;
using UnityEngine;

public class RewardIndicator : MonoBehaviour
{
    private const float OrbitRadius = 1.5f;
    private const float HideDistance = 2f;
    private const float PulseSpeed = 3f;
    private const float MinAlpha = 0.4f;
    private const float MaxAlpha = 0.9f;
    private const int ArrowTextureSize = 32;
    private const int SortOrder = 100;

    private Transform m_player;
    private List<GameObject> m_targets;
    private SpriteRenderer m_spriteRenderer;

    private static Sprite s_arrowSprite;

    public void Initialize(Transform player, List<GameObject> targets)
    {
        m_player = player;
        m_targets = new List<GameObject>(targets);

        m_spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        if (s_arrowSprite == null)
            s_arrowSprite = CreateArrowSprite();

        m_spriteRenderer.sprite = s_arrowSprite;
        m_spriteRenderer.color = new Color(1f, 0.85f, 0.2f, MaxAlpha);
        m_spriteRenderer.sortingOrder = SortOrder;

        transform.localScale = Vector3.one * 0.5f;
    }

    private void Update()
    {
        if (m_player == null) return;

        m_targets.RemoveAll(t => t == null);

        if (m_targets.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        GameObject nearest = FindNearestTarget();
        if (nearest == null) return;

        float dist = Vector2.Distance(m_player.position, nearest.transform.position);

        if (dist < HideDistance)
        {
            m_spriteRenderer.enabled = false;
            return;
        }

        m_spriteRenderer.enabled = true;

        Vector2 direction = ((Vector2)nearest.transform.position - (Vector2)m_player.position).normalized;
        transform.position = (Vector2)m_player.position + direction * OrbitRadius;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

        float alpha = Mathf.Lerp(MinAlpha, MaxAlpha, (Mathf.Sin(Time.time * PulseSpeed) + 1f) / 2f);
        Color c = m_spriteRenderer.color;
        c.a = alpha;
        m_spriteRenderer.color = c;
    }

    private GameObject FindNearestTarget()
    {
        GameObject nearest = null;
        float nearestDist = float.MaxValue;

        foreach (GameObject target in m_targets)
        {
            float dist = Vector2.Distance(m_player.position, target.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = target;
            }
        }

        return nearest;
    }

    private static Sprite CreateArrowSprite()
    {
        Texture2D tex = new Texture2D(ArrowTextureSize, ArrowTextureSize, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[ArrowTextureSize * ArrowTextureSize];
        tex.SetPixels(pixels);

        float center = ArrowTextureSize / 2f;
        int baseY = 4;
        int tipY = ArrowTextureSize - 2;

        for (int y = baseY; y <= tipY; y++)
        {
            float t = (float)(y - baseY) / (tipY - baseY);
            float halfWidth = (1f - t) * (center - 2f);

            for (int x = 0; x < ArrowTextureSize; x++)
            {
                if (Mathf.Abs(x - center) <= halfWidth)
                {
                    tex.SetPixel(x, y, Color.white);
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, ArrowTextureSize, ArrowTextureSize), new Vector2(0.5f, 0.5f), ArrowTextureSize);
    }
}
