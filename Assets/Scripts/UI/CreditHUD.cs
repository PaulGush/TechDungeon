using TMPro;
using UnityEngine;
using UnityServiceLocator;

public class CreditHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_creditText;

    private CreditManager m_creditManager;

    private void Start()
    {
        ServiceLocator.Global.TryGet(out m_creditManager);
        if (m_creditManager == null) return;

        m_creditManager.OnCreditsChanged += UpdateDisplay;
        UpdateDisplay(m_creditManager.Credits);
    }

    private void OnDestroy()
    {
        if (m_creditManager != null)
            m_creditManager.OnCreditsChanged -= UpdateDisplay;
    }

    private void UpdateDisplay(int credits)
    {
        m_creditText.text = $"CR: {credits}";
    }
}
