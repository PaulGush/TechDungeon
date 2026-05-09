using TMPro;
using UI.InputPrompts;
using UnityEngine;
using UnityServiceLocator;

namespace PlayerObject
{
    public class PlayerInteractionDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshPro m_text;
        [SerializeField] private TMP_SpriteAsset m_spriteAsset;

        private Object m_currentSource;
        private string m_currentTemplate;

        private void Awake()
        {
            m_text.enabled = false;
            if (m_spriteAsset != null) m_text.spriteAsset = m_spriteAsset;
            ServiceLocator.Global.Register(this);
        }

        private void OnEnable()
        {
            ActiveDeviceTracker.DeviceChanged += OnDeviceChanged;
        }

        private void OnDisable()
        {
            ActiveDeviceTracker.DeviceChanged -= OnDeviceChanged;
        }

        public void Show(string text, Object source)
        {
            m_currentSource = source;
            m_currentTemplate = text;
            Render();
            m_text.enabled = true;
        }

        public void Hide(Object source)
        {
            if (m_currentSource != source) return;
            m_text.enabled = false;
            m_currentSource = null;
            m_currentTemplate = null;
        }

        public void UpdateText(string text, Object source)
        {
            if (m_currentSource != source) return;
            m_currentTemplate = text;
            Render();
        }

        private void OnDeviceChanged(ActiveDevice _) => Render();

        private void Render()
        {
            if (string.IsNullOrEmpty(m_currentTemplate)) return;
            m_text.text = ButtonPromptParser.Format(m_currentTemplate, ActiveDeviceTracker.Current);
        }
    }
}
