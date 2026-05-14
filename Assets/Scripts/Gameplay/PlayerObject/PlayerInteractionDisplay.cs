using System.Collections.Generic;
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

        private struct Entry
        {
            public Object Source;
            public string Template;
        }

        // Source-keyed stack so overlapping callers (e.g. two pickups in range at once) don't
        // clobber each other — Hide(source) falls back to whichever entry is still active.
        private readonly List<Entry> m_stack = new();

        /// <summary>
        /// Transform of the prompt text — a fixed point near the player. Use as a tooltip anchor so
        /// the tooltip stays put instead of riding a bobbing pickup.
        /// </summary>
        public Transform PromptAnchor => m_text != null ? m_text.transform : transform;

        /// <summary>
        /// The source whose prompt is currently rendered (top of the stack), or null if no prompt
        /// is shown. Interactables should gate their input handlers on this so overlapping
        /// in-range targets don't all fire at once.
        /// </summary>
        public Object CurrentSource => m_stack.Count == 0 ? null : m_stack[m_stack.Count - 1].Source;

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
            int existing = IndexOf(source);
            if (existing >= 0)
            {
                m_stack[existing] = new Entry { Source = source, Template = text };
            }
            else
            {
                m_stack.Add(new Entry { Source = source, Template = text });
            }
            Render();
            m_text.enabled = true;
        }

        public void Hide(Object source)
        {
            int existing = IndexOf(source);
            if (existing < 0) return;
            m_stack.RemoveAt(existing);
            if (m_stack.Count == 0)
            {
                m_text.enabled = false;
                m_text.text = string.Empty;
            }
            else
            {
                Render();
            }
        }

        public void UpdateText(string text, Object source)
        {
            int existing = IndexOf(source);
            if (existing < 0) return;
            m_stack[existing] = new Entry { Source = source, Template = text };
            if (existing == m_stack.Count - 1) Render();
        }

        private int IndexOf(Object source)
        {
            for (int i = 0; i < m_stack.Count; i++)
            {
                if (m_stack[i].Source == source) return i;
            }
            return -1;
        }

        private void OnDeviceChanged(ActiveDevice _) => Render();

        private void Render()
        {
            if (m_stack.Count == 0) return;
            string template = m_stack[m_stack.Count - 1].Template;
            if (string.IsNullOrEmpty(template)) return;
            m_text.text = ButtonPromptParser.Format(template, ActiveDeviceTracker.Current);
        }
    }
}
