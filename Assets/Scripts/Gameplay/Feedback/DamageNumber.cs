using System.Collections;
using Gameplay.ObjectPool;
using TMPro;
using UnityEngine;
using UnityServiceLocator;

public class DamageNumber : MonoBehaviour
{
    [SerializeField] private TextMeshPro m_label;

    [Header("Animation")]
    [Tooltip("Total seconds the number is visible before returning to pool.")]
    [SerializeField] private float m_lifetime = 0.6f;

    [Tooltip("World units the number rises during its lifetime.")]
    [SerializeField] private float m_riseDistance = 1f;

    [Tooltip("Scale multiplier at the peak of the pop at spawn. 1 disables the pop.")]
    [SerializeField] private float m_popScale = 1.3f;

    [Tooltip("Seconds spent expanding from base scale to PopScale at spawn.")]
    [SerializeField] private float m_popDuration = 0.08f;

    [Tooltip("Fraction of the lifetime (0..1) spent fully visible before alpha fade begins.")]
    [Range(0f, 1f)]
    [SerializeField] private float m_fadeStart = 0.5f;

    private ObjectPool m_pool;
    private Vector3 m_baseScale;
    private Coroutine m_routine;

    private void Awake()
    {
        ServiceLocator.Global.TryGet(out m_pool);
        m_baseScale = transform.localScale;
        if (m_label == null)
            m_label = GetComponentInChildren<TextMeshPro>();
    }

    private void OnDisable()
    {
        if (m_routine != null)
        {
            StopCoroutine(m_routine);
            m_routine = null;
        }
    }

    public void Play(int amount, Color color)
    {
        if (m_label == null) return;

        m_label.text = amount.ToString();
        m_label.color = color;
        transform.localScale = m_baseScale;

        if (m_routine != null) StopCoroutine(m_routine);
        m_routine = StartCoroutine(AnimateRoutine(color));
    }

    private IEnumerator AnimateRoutine(Color baseColor)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.up * m_riseDistance;

        float elapsed = 0f;
        while (elapsed < m_lifetime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / m_lifetime);

            // Ease-out rise so the number flies up quickly and decelerates.
            float riseEase = 1f - (1f - t) * (1f - t);
            transform.position = Vector3.Lerp(startPos, endPos, riseEase);

            // Scale pop during m_popDuration, then settle back to 1x over the rest.
            float scaleMult;
            if (elapsed < m_popDuration && m_popDuration > 0f)
                scaleMult = Mathf.Lerp(1f, m_popScale, elapsed / m_popDuration);
            else if (m_popDuration > 0f)
                scaleMult = Mathf.Lerp(m_popScale, 1f, Mathf.Clamp01((elapsed - m_popDuration) / (m_lifetime - m_popDuration)));
            else
                scaleMult = 1f;
            transform.localScale = m_baseScale * scaleMult;

            // Alpha fade kicks in after m_fadeStart of the lifetime has passed.
            Color c = baseColor;
            c.a = t < m_fadeStart ? 1f : 1f - Mathf.InverseLerp(m_fadeStart, 1f, t);
            m_label.color = c;

            yield return null;
        }

        m_routine = null;

        if (m_pool == null || !m_pool.ReturnGameObject(gameObject))
            Destroy(gameObject);
    }
}
