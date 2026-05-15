using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI.InputPrompts
{
    [CreateAssetMenu(fileName = "ButtonPromptDatabase", menuName = "TechDungeon/Button Prompt Database")]
    public class ButtonPromptDatabase : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("Action name as defined in InputSystem_Actions (e.g. Interact, AltInteract, Roll).")]
            public string actionName;
            public Sprite keyboardMouse;
            public Sprite xbox;
            public Sprite playStation;
            public Sprite nintendoSwitch;

            public Sprite Get(ActiveDevice device) => device switch
            {
                ActiveDevice.KeyboardMouse => keyboardMouse,
                ActiveDevice.Xbox => xbox,
                ActiveDevice.PlayStation => playStation,
                ActiveDevice.Switch => nintendoSwitch,
                _ => keyboardMouse,
            };
        }

        public Entry[] entries;

        private Dictionary<string, Entry> m_index;

        private void OnEnable() => m_index = null;

        public bool TryGet(string actionName, out Entry entry)
        {
            if (m_index == null) BuildIndex();
            return m_index.TryGetValue(actionName, out entry);
        }

        private void BuildIndex()
        {
            m_index = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
            if (entries == null) return;
            foreach (Entry e in entries)
            {
                if (e == null || string.IsNullOrWhiteSpace(e.actionName)) continue;
                m_index[e.actionName] = e;
            }
        }

        public static string SpriteName(string actionName, ActiveDevice device) =>
            $"{actionName.ToLowerInvariant()}_{DeviceSuffix(device)}";

        public static string DeviceSuffix(ActiveDevice device) => device switch
        {
            ActiveDevice.KeyboardMouse => "kbm",
            ActiveDevice.Xbox => "xbox",
            ActiveDevice.PlayStation => "ps",
            ActiveDevice.Switch => "switch",
            _ => "kbm",
        };
    }
}
