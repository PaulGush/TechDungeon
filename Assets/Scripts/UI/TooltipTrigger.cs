using UnityEngine;
using UnityEngine.EventSystems;
using UnityServiceLocator;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    private string m_title;
    private string m_body;
    private string m_effect;
    private Tooltip m_tooltip;
    private RectTransform m_rect;

    public void Setup(string title, string body, string effect)
    {
        m_title = title;
        m_body = body;
        m_effect = effect;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowTooltip(useAnchor: false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        m_tooltip?.Hide();
    }

    public void OnSelect(BaseEventData eventData)
    {
        ShowTooltip(useAnchor: true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        m_tooltip?.Hide();
    }

    private void ShowTooltip(bool useAnchor)
    {
        if (m_tooltip == null)
            ServiceLocator.Global.TryGet(out m_tooltip);
        if (m_tooltip == null) return;

        if (useAnchor)
        {
            if (m_rect == null) m_rect = transform as RectTransform;
            if (m_rect != null)
            {
                m_tooltip.Show(m_title, m_body, m_effect, m_rect);
                return;
            }
        }
        m_tooltip.Show(m_title, m_body, m_effect);
    }

    private void OnDisable()
    {
        m_tooltip?.Hide();
    }
}
