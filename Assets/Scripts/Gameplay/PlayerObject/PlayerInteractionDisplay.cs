using TMPro;
using UnityEngine;
using UnityServiceLocator;

namespace PlayerObject
{
    public class PlayerInteractionDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshPro m_text;

        private Object m_currentSource;

        private void Awake()
        {
            m_text.enabled = false;
            ServiceLocator.Global.Register(this);
        }

        public void Show(string text, Object source)
        {
            m_currentSource = source;
            m_text.text = text;
            m_text.enabled = true;
        }

        public void Hide(Object source)
        {
            if (m_currentSource != source) return;
            m_text.enabled = false;
            m_currentSource = null;
        }

        public void UpdateText(string text, Object source)
        {
            if (m_currentSource != source) return;
            m_text.text = text;
        }
    }
}
