using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityServiceLocator;

public class Tooltip : MonoBehaviour
{
    private enum AnchorMode { Mouse, UI, World, Centered, UIAbove }

    [Header("References")]
    [SerializeField] private RectTransform m_panel;
    [SerializeField] private CanvasGroup m_canvasGroup;
    [SerializeField] private TextMeshProUGUI m_titleText;
    [SerializeField] private TextMeshProUGUI m_bodyText;
    [SerializeField] private TextMeshProUGUI m_effectText;

    [Header("Positioning")]
    [Tooltip("Offset from the anchor in screen pixels. Positive X is to the right, positive Y is above.")]
    [SerializeField] private Vector2 m_cursorOffset = new Vector2(16f, -16f);

    [Tooltip("Vertical gap in screen pixels between the anchor and the bottom of the tooltip in UIAbove mode.")]
    [SerializeField] private float m_aboveGap = 12f;

    [Tooltip("Inset from the canvas edges when clamping the tooltip on-screen, in screen pixels. Keeps a visible gap so the panel never touches the edge.")]
    [SerializeField] private Vector2 m_screenMargin = new Vector2(16f, 16f);

    private struct Entry
    {
        public Object Source;
        public string Title;
        public string Body;
        public string Effect;
        public AnchorMode Mode;
        public RectTransform UiAnchor;
        public Transform WorldAnchor;
    }

    // One PanelInstance per concurrently visible tooltip. The original m_panel is the template;
    // additional sources clone it so e.g. an ability hover and a pickup prompt can coexist.
    private class PanelInstance
    {
        public RectTransform Rect;
        public TextMeshProUGUI Title;
        public TextMeshProUGUI Body;
        public TextMeshProUGUI Effect;
        public Vector2 HomeAnchoredPosition;  // panel's authored position; restored for Centered mode
        public bool IsOriginal;
    }

    private struct ActiveSlot
    {
        public Entry Data;
        public PanelInstance Panel;
    }

    // Index-aligned: one slot per active source. Linear search is fine here — N is tiny (1-3 typically).
    private readonly List<ActiveSlot> m_active = new();
    private readonly Stack<PanelInstance> m_pool = new();

    private PanelInstance m_originalPanel;
    private RectTransform m_parentRect;
    private Canvas m_canvas;

    private void Awake()
    {
        ServiceLocator.Global.Register(this);

        m_parentRect = m_panel.parent as RectTransform;
        m_canvas = GetComponentInParent<Canvas>();

        m_originalPanel = new PanelInstance
        {
            Rect = m_panel,
            Title = m_titleText,
            Body = m_bodyText,
            Effect = m_effectText,
            HomeAnchoredPosition = m_panel.anchoredPosition,
            IsOriginal = true,
        };
        m_pool.Push(m_originalPanel);

        // Hide the original panel at start — it sits visible in the scene for design purposes.
        SetPanelVisible(m_originalPanel, false);

        if (m_canvasGroup != null)
        {
            m_canvasGroup.alpha = 1f;
            m_canvasGroup.blocksRaycasts = false;
        }
    }

    public void Show(string title, string body, string effect)
        => Push(null, title, body, effect, AnchorMode.Mouse, null, null);

    public void Show(string title, string body, string effect, RectTransform uiAnchor)
        => Push(null, title, body, effect, AnchorMode.UI, uiAnchor, null);

    public void Show(string title, string body, string effect, RectTransform uiAnchor, Object source)
        => Push(source, title, body, effect, AnchorMode.UI, uiAnchor, null);

    // Centers the panel horizontally on the anchor's pivot and places its bottom edge above it.
    // Use this for UI rows (e.g. ability bar) where the tooltip should hover over the group rather
    // than follow the cursor offset.
    public void ShowAbove(string title, string body, string effect, RectTransform uiAnchor, Object source)
        => Push(source, title, body, effect, AnchorMode.UIAbove, uiAnchor, null);

    public void Show(string title, string body, string effect, Transform worldAnchor)
        => Push(null, title, body, effect, AnchorMode.World, null, worldAnchor);

    public void Show(string title, string body, string effect, Transform worldAnchor, Object source)
        => Push(source, title, body, effect, AnchorMode.World, null, worldAnchor);

    // Centered: no positioning is applied at runtime; the panel sits wherever its prefab anchors
    // place it. Use this when you want the panel to be fixed on screen (e.g. tooltip prefab
    // anchored to the centre of the canvas) rather than following an in-world target.
    public void Show(string title, string body, string effect, Object source)
        => Push(source, title, body, effect, AnchorMode.Centered, null, null);

    public void Hide()
    {
        for (int i = m_active.Count - 1; i >= 0; i--)
            Release(m_active[i].Panel);
        m_active.Clear();
    }

    public void Hide(Object source)
    {
        int idx = IndexOf(source);
        if (idx < 0) return;
        Release(m_active[idx].Panel);
        m_active.RemoveAt(idx);
    }

    private void Push(Object source, string title, string body, string effect, AnchorMode mode, RectTransform uiAnchor, Transform worldAnchor)
    {
        Entry entry = new Entry
        {
            Source = source,
            Title = title,
            Body = body,
            Effect = effect,
            Mode = mode,
            UiAnchor = uiAnchor,
            WorldAnchor = worldAnchor,
        };

        int idx = IndexOf(source);
        ActiveSlot slot;
        if (idx >= 0)
        {
            slot = m_active[idx];
            slot.Data = entry;
            m_active[idx] = slot;
        }
        else
        {
            slot = new ActiveSlot { Data = entry, Panel = Acquire() };
            m_active.Add(slot);
        }

        ApplyContent(slot.Panel, entry);
        SetPanelVisible(slot.Panel, true);
        // Reset to home before applying mode-specific position; otherwise a panel reused from a
        // prior non-Centered show would still sit at its old position when Centered is requested.
        if (mode == AnchorMode.Centered)
            slot.Panel.Rect.anchoredPosition = slot.Panel.HomeAnchoredPosition;
        UpdatePosition(slot.Panel, entry);
    }

    private void ApplyContent(PanelInstance p, Entry e)
    {
        if (p.Title != null) p.Title.text = e.Title;
        if (p.Body != null) p.Body.text = e.Body;
        if (p.Effect != null) p.Effect.text = e.Effect;
    }

    private PanelInstance Acquire()
    {
        if (m_pool.Count > 0) return m_pool.Pop();
        return Clone();
    }

    private void Release(PanelInstance panel)
    {
        SetPanelVisible(panel, false);
        m_pool.Push(panel);
    }

    private PanelInstance Clone()
    {
        GameObject cloneGo = Instantiate(m_panel.gameObject, m_panel.parent);
        cloneGo.name = m_panel.name + " (Tooltip Clone)";
        RectTransform rect = cloneGo.GetComponent<RectTransform>();
        return new PanelInstance
        {
            Rect = rect,
            Title = FindMatchingByName(rect, m_titleText),
            Body = FindMatchingByName(rect, m_bodyText),
            Effect = FindMatchingByName(rect, m_effectText),
            HomeAnchoredPosition = m_originalPanel.HomeAnchoredPosition,
            IsOriginal = false,
        };
    }

    // Finds the cloned-hierarchy counterpart of an original by matching the GameObject name.
    private static TextMeshProUGUI FindMatchingByName(Transform cloneRoot, TextMeshProUGUI original)
    {
        if (original == null) return null;
        string name = original.gameObject.name;
        TextMeshProUGUI[] all = cloneRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI t in all)
            if (t.gameObject.name == name) return t;
        return null;
    }

    private static void SetPanelVisible(PanelInstance p, bool visible)
    {
        if (p?.Rect != null && p.Rect.gameObject.activeSelf != visible)
            p.Rect.gameObject.SetActive(visible);
    }

    private int IndexOf(Object source)
    {
        for (int i = 0; i < m_active.Count; i++)
            if (m_active[i].Data.Source == source) return i;
        return -1;
    }

    private void LateUpdate()
    {
        // Anchors (UI, World) can move each frame — keep panel positions in sync.
        for (int i = 0; i < m_active.Count; i++)
            UpdatePosition(m_active[i].Panel, m_active[i].Data);
    }

    private void UpdatePosition(PanelInstance panel, Entry entry)
    {
        if (m_parentRect == null || panel?.Rect == null) return;
        // Centered mode: leave the panel where the prefab anchored it.
        if (entry.Mode == AnchorMode.Centered) return;
        if (!TryGetAnchorScreenPoint(entry, out Vector2 screenPos)) return;

        // Camera is null for ScreenSpaceOverlay; required for ScreenSpaceCamera/WorldSpace.
        Camera cam = m_canvas != null && m_canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? m_canvas.worldCamera
            : null;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(m_parentRect, screenPos, cam, out Vector2 localPoint))
            return;

        Vector2 panelSize = panel.Rect.rect.size;
        Vector2 panelPivot = panel.Rect.pivot;
        Rect parentRect = m_parentRect.rect;

        Vector2 anchoredPos;
        if (entry.Mode == AnchorMode.UIAbove)
        {
            // Center horizontally on the anchor and place the panel's bottom edge above it,
            // using the panel's actual pivot rather than assuming (0, 1).
            anchoredPos.x = localPoint.x + (panelPivot.x - 0.5f) * panelSize.x;
            anchoredPos.y = localPoint.y + m_aboveGap + panelPivot.y * panelSize.y;
        }
        else
        {
            Vector2 offset = m_cursorOffset;

            // Flip to the left of the cursor if we'd overflow the right edge.
            if (localPoint.x + offset.x + panelSize.x > parentRect.xMax)
                offset.x = -m_cursorOffset.x - panelSize.x;

            // Flip above the cursor if we'd overflow the bottom edge.
            if (localPoint.y + offset.y - panelSize.y < parentRect.yMin)
                offset.y = -m_cursorOffset.y + panelSize.y;

            anchoredPos = localPoint + offset;
        }

        // Final clamp for edge cases where the panel is larger than the remaining space on
        // either side of the cursor. Keeps the whole panel on screen even if the chosen side
        // doesn't actually fit. Uses the panel's actual pivot to translate edge positions back
        // into anchoredPosition (formerly assumed pivot (0, 1)).
        float minX = parentRect.xMin + m_screenMargin.x + panelPivot.x * panelSize.x;
        float maxX = parentRect.xMax - m_screenMargin.x - (1f - panelPivot.x) * panelSize.x;
        float minY = parentRect.yMin + m_screenMargin.y + panelPivot.y * panelSize.y;
        float maxY = parentRect.yMax - m_screenMargin.y - (1f - panelPivot.y) * panelSize.y;
        if (maxX >= minX) anchoredPos.x = Mathf.Clamp(anchoredPos.x, minX, maxX);
        if (maxY >= minY) anchoredPos.y = Mathf.Clamp(anchoredPos.y, minY, maxY);

        panel.Rect.anchoredPosition = anchoredPos;
    }

    private bool TryGetAnchorScreenPoint(Entry entry, out Vector2 screenPos)
    {
        switch (entry.Mode)
        {
            case AnchorMode.UI:
            case AnchorMode.UIAbove:
                if (entry.UiAnchor == null) { screenPos = default; return false; }
                Camera uiCam = m_canvas != null && m_canvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? m_canvas.worldCamera
                    : null;
                screenPos = RectTransformUtility.WorldToScreenPoint(uiCam, entry.UiAnchor.position);
                return true;
            case AnchorMode.World:
                if (entry.WorldAnchor == null) { screenPos = default; return false; }
                Camera mainCam = Camera.main;
                if (mainCam == null) { screenPos = default; return false; }
                screenPos = mainCam.WorldToScreenPoint(entry.WorldAnchor.position);
                return true;
            default:
                if (Mouse.current == null) { screenPos = default; return false; }
                screenPos = Mouse.current.position.ReadValue();
                return true;
        }
    }
}
