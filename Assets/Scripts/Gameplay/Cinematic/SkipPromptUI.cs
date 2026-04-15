using UnityEngine;
using UnityEngine.UI;

public class SkipPromptUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup m_canvasGroup;
    [SerializeField] private Image m_fillImage;

    public void SetProgress(float t)
    {
        if (m_canvasGroup != null)
            m_canvasGroup.alpha = 1f;
        if (m_fillImage != null)
            m_fillImage.fillAmount = Mathf.Clamp01(t);
    }

    public void Hide()
    {
        if (m_canvasGroup != null)
            m_canvasGroup.alpha = 0f;
        if (m_fillImage != null)
            m_fillImage.fillAmount = 0f;
    }
}
