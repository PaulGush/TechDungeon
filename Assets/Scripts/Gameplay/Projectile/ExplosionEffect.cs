using System.Collections;
using UnityEngine;

public class ExplosionEffect : MonoBehaviour
{
    [SerializeField] private SpriteRenderer m_spriteRenderer;
    [SerializeField] private float m_duration = 0.3f;

    private void OnEnable()
    {
        StartCoroutine(AnimateExplosion());
    }

    private IEnumerator AnimateExplosion()
    {
        float elapsed = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = transform.localScale;
        Color color = m_spriteRenderer.color;

        while (elapsed < m_duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / m_duration;

            transform.localScale = Vector3.Lerp(startScale, endScale, t);

            color.a = 1f - t;
            m_spriteRenderer.color = color;

            yield return null;
        }

        Destroy(gameObject);
    }
}
