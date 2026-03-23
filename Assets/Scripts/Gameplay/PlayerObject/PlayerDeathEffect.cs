using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerObject
{
    public class PlayerDeathEffect : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EntityHealth m_health;
        [SerializeField] private Transform m_playerTransform;
        [SerializeField] private RawImage m_cutoutImage;
        [SerializeField] private Camera m_camera;

        [Header("Timing")]
        [SerializeField] private float m_effectDuration = 2f;
        [SerializeField] private float m_slowMoTimeScale = 0.1f;

        [Header("Circle Cutout")]
        [SerializeField] private Color m_overlayColor = new Color(0, 0, 0, 0.85f);
        [SerializeField] private float m_startRadius = 1.5f;
        [SerializeField] private float m_endRadius = 0.08f;
        [SerializeField] private float m_edgeSoftness = 0.02f;

        private Material m_cutoutMaterial;
        private bool m_isDying;

        private static readonly int CenterID = Shader.PropertyToID("_Center");
        private static readonly int RadiusID = Shader.PropertyToID("_Radius");
        private static readonly int SoftnessID = Shader.PropertyToID("_Softness");
        private static readonly int ColorID = Shader.PropertyToID("_Color");

        private void OnEnable()
        {
            m_health.OnDeath += OnPlayerDeath;
        }

        private void OnDisable()
        {
            m_health.OnDeath -= OnPlayerDeath;
        }

        private void Start()
        {
            var shader = Shader.Find("UI/CircleCutout");
            m_cutoutMaterial = new Material(shader);
            m_cutoutImage.material = m_cutoutMaterial;
            m_cutoutImage.gameObject.SetActive(false);
        }

        private void OnPlayerDeath()
        {
            if (m_isDying)
                return;

            m_isDying = true;
            StartCoroutine(DeathSequence());
        }

        private IEnumerator DeathSequence()
        {
            m_cutoutImage.gameObject.SetActive(true);

            m_cutoutMaterial.SetFloat(SoftnessID, m_edgeSoftness);
            m_cutoutMaterial.SetFloat(RadiusID, m_startRadius);
            m_cutoutMaterial.SetColor(ColorID, m_overlayColor);

            float elapsed = 0f;
            float originalTimeScale = Time.timeScale;

            while (elapsed < m_effectDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / m_effectDuration);
                float easedT = t * t;

                Time.timeScale = Mathf.Lerp(originalTimeScale, m_slowMoTimeScale, t);
                Time.fixedDeltaTime = 0.02f * Time.timeScale;

                Vector3 screenPos = m_camera.WorldToViewportPoint(m_playerTransform.position);
                m_cutoutMaterial.SetVector(CenterID, new Vector4(screenPos.x, screenPos.y, 0, 0));

                float radius = Mathf.Lerp(m_startRadius, m_endRadius, easedT);
                m_cutoutMaterial.SetFloat(RadiusID, radius);

                yield return null;
            }

            Time.timeScale = m_slowMoTimeScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            m_cutoutMaterial.SetFloat(RadiusID, m_endRadius);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }
    }
}
