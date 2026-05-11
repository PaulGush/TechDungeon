using UI.InputPrompts;
using UnityEngine;
using UnityEngine.UI;

public class AbilityHudBar : MonoBehaviour
{
    [Tooltip("Layout group enabled when keyboard&mouse is active so it arranges the slots in a horizontal row. Should be DISABLED in the scene so the authored RectTransform positions render as the controller D-pad cross.")]
    [SerializeField] private HorizontalLayoutGroup m_keyboardLayout;

    [Tooltip("Slots in slot-index order (0..3). Full RectTransform state (anchors, pivot, anchoredPosition, sizeDelta) is captured on Awake before the layout group ever runs, then restored when a controller becomes active.")]
    [SerializeField] private RectTransform[] m_slots;

    private struct RectSnapshot
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
    }

    private RectSnapshot[] m_dpadSnapshots;

    private void Awake()
    {
        if (m_slots == null) return;
        m_dpadSnapshots = new RectSnapshot[m_slots.Length];
        for (int i = 0; i < m_slots.Length; i++)
        {
            if (m_slots[i] == null) continue;
            m_dpadSnapshots[i] = new RectSnapshot
            {
                anchorMin = m_slots[i].anchorMin,
                anchorMax = m_slots[i].anchorMax,
                pivot = m_slots[i].pivot,
                anchoredPosition = m_slots[i].anchoredPosition,
                sizeDelta = m_slots[i].sizeDelta,
            };
        }
    }

    private void OnEnable()
    {
        ActiveDeviceTracker.DeviceChanged += OnDeviceChanged;
        Apply(ActiveDeviceTracker.Current);
    }

    private void OnDisable()
    {
        ActiveDeviceTracker.DeviceChanged -= OnDeviceChanged;
    }

    private void OnDeviceChanged(ActiveDevice device) => Apply(device);

    private void Apply(ActiveDevice device)
    {
        bool keyboard = device == ActiveDevice.KeyboardMouse;

        if (keyboard)
        {
            if (m_keyboardLayout != null) m_keyboardLayout.enabled = true;
            return;
        }

        if (m_keyboardLayout != null) m_keyboardLayout.enabled = false;
        RestoreDpadCross();
    }

    private void RestoreDpadCross()
    {
        if (m_slots == null || m_dpadSnapshots == null) return;
        for (int i = 0; i < m_slots.Length; i++)
        {
            if (m_slots[i] == null) continue;
            RectSnapshot s = m_dpadSnapshots[i];
            // Anchors before position so anchoredPosition is interpreted in the original anchor frame.
            m_slots[i].anchorMin = s.anchorMin;
            m_slots[i].anchorMax = s.anchorMax;
            m_slots[i].pivot = s.pivot;
            m_slots[i].anchoredPosition = s.anchoredPosition;
            m_slots[i].sizeDelta = s.sizeDelta;
        }
    }
}
