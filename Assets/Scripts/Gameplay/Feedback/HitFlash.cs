using System.Collections;
using UnityEngine;

public class HitFlash : MonoBehaviour
{
    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
    private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");

    [Header("References")]
    [SerializeField] private EntityHealth m_health;
    [Tooltip("SpriteRenderer whose material will have _FlashAmount / _FlashColor driven. The material must use the SpriteHitFlash shader for the flash to render.")]
    [SerializeField] private SpriteRenderer m_renderer;
    [Tooltip("Transform to pop in scale. Defaults to this GameObject's transform if null.")]
    [SerializeField] private Transform m_scaleTarget;

    [Header("Feel")]
    [Tooltip("Unscaled seconds the flash + scale pop holds. Pair this with HitStopOnDeath/HitStopService duration so the pop is visible during the freeze.")]
    [SerializeField] private float m_duration = 0.1f;
    [Tooltip("Scale multiplier at the peak of the pop. 1 disables the pop.")]
    [SerializeField] private float m_popScale = 1.15f;

    [Header("Colors")]
    [Tooltip("Flash color used on damage hits.")]
    [SerializeField] private Color m_hitColor = Color.white;
    [Tooltip("Flash color used when the entity heals. Set alpha to 0 to suppress heal flashes on this entity.")]
    [SerializeField] private Color m_healColor = new Color(0.4f, 1f, 0.4f, 1f);

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
        {
            m_health.OnTakeDamage += HandleHit;
            m_health.OnHeal += HandleHeal;
        }

        // Re-capture original scale in case the object was pooled and returned with a modified transform.
        if (m_scaleTarget != null)
            m_originalScale = m_scaleTarget.localScale;

        ResetVisuals();
    }

    private void OnDisable()
    {
        if (m_health != null)
        {
            m_health.OnTakeDamage -= HandleHit;
            m_health.OnHeal -= HandleHeal;
        }

        if (m_routine != null)
        {
            StopCoroutine(m_routine);
            m_routine = null;
        }

        ResetVisuals();
    }

    private void HandleHit() => Flash(m_hitColor);

    private void HandleHeal()
    {
        if (m_healColor.a <= 0f) return;
        Flash(m_healColor);
    }

    private void Flash(Color color)
    {
        if (m_routine != null) StopCoroutine(m_routine);
        m_routine = StartCoroutine(FlashRoutine(color));
    }

    private IEnumerator FlashRoutine(Color color)
    {
        Material mat = MaterialInstance;
        if (mat != null)
        {
            mat.SetColor(FlashColorId, color);
            mat.SetFloat(FlashAmountId, 1f);
        }
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
