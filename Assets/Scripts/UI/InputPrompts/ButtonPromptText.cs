using TMPro;
using UnityEngine;

namespace UI.InputPrompts
{
    [DisallowMultipleComponent]
    public class ButtonPromptText : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_target;

        private string m_template;

        public TMP_Text Target => m_target;

        private void Reset() => m_target = GetComponent<TMP_Text>();

        private void Awake()
        {
            if (m_target == null) m_target = GetComponent<TMP_Text>();
            m_template = m_target != null ? m_target.text : string.Empty;
            Render();
        }

        private void OnEnable()
        {
            ActiveDeviceTracker.DeviceChanged += OnDeviceChanged;
            Render();
        }

        private void OnDisable()
        {
            ActiveDeviceTracker.DeviceChanged -= OnDeviceChanged;
        }

        public void SetTemplate(string template)
        {
            m_template = template ?? string.Empty;
            Render();
        }

        public string GetTemplate() => m_template;

        private void OnDeviceChanged(ActiveDevice _) => Render();

        private void Render()
        {
            if (m_target == null) return;
            m_target.text = ButtonPromptParser.Format(m_template, ActiveDeviceTracker.Current);
        }
    }
}
