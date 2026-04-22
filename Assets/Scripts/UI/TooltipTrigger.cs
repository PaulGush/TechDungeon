using UnityEngine;
using UnityEngine.EventSystems;
using UnityServiceLocator;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private string m_title;
    private string m_body;
    private string m_effect;
    private Tooltip m_tooltip;

    public void Setup(string title, string body, string effect)
    {
        m_title = title;
        m_body = body;
        m_effect = effect;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (m_tooltip == null)
            ServiceLocator.Global.TryGet(out m_tooltip);

        m_tooltip?.Show(m_title, m_body, m_effect);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        m_tooltip?.Hide();
    }

    private void OnDisable()
    {
        m_tooltip?.Hide();
    }
}
