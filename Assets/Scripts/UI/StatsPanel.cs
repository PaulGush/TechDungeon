using Input;
using UnityEngine;

public class StatsPanel : MonoBehaviour
{
    [SerializeField] private InputReader m_inputReader;
    [SerializeField] private CanvasGroup m_panelGroup;

    private void OnEnable()
    {
        m_inputReader.Inventory += Show;
        m_inputReader.InventoryReleased += Hide;
        Hide();
    }

    private void OnDisable()
    {
        m_inputReader.Inventory -= Show;
        m_inputReader.InventoryReleased -= Hide;
    }

    private void Show()
    {
        m_panelGroup.alpha = 1f;
        m_panelGroup.blocksRaycasts = true;
    }

    private void Hide()
    {
        m_panelGroup.alpha = 0f;
        m_panelGroup.blocksRaycasts = false;
    }
}
