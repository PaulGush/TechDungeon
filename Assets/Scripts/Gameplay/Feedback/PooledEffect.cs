using Gameplay.ObjectPool;
using UnityEngine;
using UnityServiceLocator;

public class PooledEffect : MonoBehaviour
{
    [Tooltip("Seconds before this effect returns itself to the ObjectPool. Match or slightly exceed the visual's animation length.")]
    [SerializeField] private float m_lifetime = 0.2f;

    [Tooltip("Optional sprite renderer to tint via SetTint. Leave empty to skip tinting.")]
    [SerializeField] private SpriteRenderer m_tintRenderer;

    private ObjectPool m_pool;
    private Color m_defaultTint = Color.white;
    private bool m_cachedDefault;

    private void Awake()
    {
        if (m_tintRenderer != null && !m_cachedDefault)
        {
            m_defaultTint = m_tintRenderer.color;
            m_cachedDefault = true;
        }
    }

    private void OnEnable()
    {
        if (m_pool == null)
            ServiceLocator.Global.TryGet(out m_pool);

        if (m_pool != null)
            StartCoroutine(m_pool.ReturnAfter(gameObject, m_lifetime));
    }

    private void OnDisable()
    {
        if (m_tintRenderer != null && m_cachedDefault)
            m_tintRenderer.color = m_defaultTint;
    }

    public void SetTint(Color color)
    {
        if (m_tintRenderer != null)
            m_tintRenderer.color = color;
    }
}
