using System.Collections;
using UnityEngine;

public class HitFlash : MonoBehaviour
{
    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");

    [Header("References")]
    [SerializeField] private EntityHealth m_health;
    [Tooltip("SpriteRenderer whose material will have _FlashAmount driven. The material must use the SpriteHitFlash shader for the flash to render.")]
    [SerializeField] private SpriteRenderer m_renderer;
    [Tooltip("Transform to pop in scale. Defaults to this GameObject's transform if null.")]
    [SerializeField] private Transform m_scaleTarget;

    [Header("Feel")]
    [Tooltip("Unscaled seconds the flash + scale pop holds. Pair this with HitStopOnDeath/HitStopService duration so the pop is visible during the freeze.")]
    [SerializeField] private float m_duration = 0.1f;
    [Tooltip("Scale multiplier at the peak of the pop. 1 disables the pop.")]
    [SerializeField] private float m_popScale = 1.15f;

    private Material m_materialInstance;
    private Vector3 m_originalScale;
    private Coroutine m_routine;

    private Material MaterialInstance
    {
        get
        {
            if (m_materialInstance == null && m_renderer != null)
                m_materialInstance = m_renderer.material;
            return m_materialInstance;
        }
    }

    private void Awake()
    {
        if (m_scaleTarget == null) m_scaleTarget = transform;
        m_originalScale = m_scaleTarget.localScale;
    }

    private void OnEnable()
    {
        if (m_health != null)
            m_health.OnTakeDamage += HandleHit;

        // Re-capture original scale in case the object was pooled and returned with a modified transform.
        if (m_scaleTarget != null)
            m_originalScale = m_scaleTarget.localScale;

        ResetVisuals();
    }

    private void OnDisable()
    {
        if (m_health != null)
            m_health.OnTakeDamage -= HandleHit;

        if (m_routine != null)
        {
            StopCoroutine(m_routine);
            m_routine = null;
        }

        ResetVisuals();
    }

    private void HandleHit()
    {
        if (m_routine != null) StopCoroutine(m_routine);
        m_routine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        Material mat = MaterialInstance;
        mat?.SetFloat(FlashAmountId, 1f);
        if (m_scaleTarget != null)
            m_scaleTarget.localScale = m_originalScale * m_popScale;

        // WaitForSecondsRealtime so the flash still reads correctly while HitStopService
        // has Time.timeScale pinned near zero.
        yield return new WaitForSecondsRealtime(m_duration);

        ResetVisuals();
        m_routine = null;
    }

    private void ResetVisuals()
    {
        if (m_materialInstance != null)
            m_materialInstance.SetFloat(FlashAmountId, 0f);
        if (m_scaleTarget != null)
            m_scaleTarget.localScale = m_originalScale;
    }
}
