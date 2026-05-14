using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityServiceLocator;

public class Tooltip : MonoBehaviour
{
    private enum AnchorMode { Mouse, UI, World, Centered }

    [Header("References")]
    [SerializeField] private RectTransform m_panel;
    [SerializeField] private CanvasGroup m_canvasGroup;
    [SerializeField] private TextMeshProUGUI m_titleText;
    [SerializeField] private TextMeshProUGUI m_bodyText;
    [SerializeField] private TextMeshProUGUI m_effectText;

    [Header("Positioning")]
    [Tooltip("Offset from the anchor in screen pixels. Positive X is to the right, positive Y is above.")]
    [SerializeField] private Vector2 m_cursorOffset = new Vector2(16f, -16f);

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

    // Source-keyed stack so overlapping callers (e.g. two pickups) don't clobber each other.
    // Sourceless callers share the null slot.
    private readonly List<Entry> m_stack = new();

    private RectTransform m_parentRect;
    private Canvas m_canvas;

    private void Awake()
    {
        ServiceLocator.Global.Register(this);

        m_parentRect = m_panel.parent as RectTransform;
        m_canvas = GetComponentInParent<Canvas>();

        HideImmediate();
    }

    public void Show(string title, string body, string effect)
        => Push(null, title, body, effect, AnchorMode.Mouse, null, null);

    public void Show(string title, string body, string effect, RectTransform uiAnchor)
        => Push(null, title, body, effect, AnchorMode.UI, uiAnchor, null);

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
        m_stack.Clear();
        ApplyTopOrFade();
    }

    public void Hide(Object source)
    {
        int idx = IndexOf(source);
        if (idx < 0) return;
        m_stack.RemoveAt(idx);
        ApplyTopOrFade();
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
        if (idx >= 0) m_stack[idx] = entry;
        else m_stack.Add(entry);

        ApplyTop();
    }

    private void ApplyTop()
    {
        if (m_stack.Count == 0) return;
        Entry top = m_stack[m_stack.Count - 1];
        if (m_titleText != null) m_titleText.text = top.Title;
        if (m_bodyText != null) m_bodyText.text = top.Body;
        if (m_effectText != null) m_effectText.text = top.Effect;
        m_canvasGroup.alpha = 1f;
        m_canvasGroup.blocksRaycasts = false;
        UpdatePosition();
    }

    private void ApplyTopOrFade()
    {
        if (m_stack.Count > 0)
        {
            ApplyTop();
        }
        else
        {
            m_canvasGroup.alpha = 0f;
            m_canvasGroup.blocksRaycasts = false;
        }
    }

    private int IndexOf(Object source)
    {
        for (int i = 0; i < m_stack.Count; i++)
        {
            if (m_stack[i].Source == source) return i;
        }
        return -1;
    }

    private void HideImmediate() => Hide();

    private void LateUpdate()
    {
        if (m_stack.Count > 0) UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (m_parentRect == null) return;
        if (m_stack.Count == 0) return;
        Entry top = m_stack[m_stack.Count - 1];
        // Centered mode: leave the panel where the prefab anchored it.
        if (top.Mode == AnchorMode.Centered) return;
        if (!TryGetAnchorScreenPoint(top, out Vector2 screenPos)) return;

        // Camera is null for ScreenSpaceOverlay; required for ScreenSpaceCamera/WorldSpace.
        Camera cam = m_canvas != null && m_canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? m_canvas.worldCamera
            : null;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(m_parentRect, screenPos, cam, out Vector2 localPoint))
            return;

        // Panel pivot is assumed (0, 1): top-left corner sits at anchoredPosition, the panel
        // extends right by panelSize.x and down by panelSize.y from there.
        Vector2 offset = m_cursorOffset;
        Vector2 panelSize = m_panel.rect.size;
        Rect parentRect = m_parentRect.rect;

        // Flip to the left of the cursor if we'd overflow the right edge.
        if (localPoint.x + offset.x + panelSize.x > parentRect.xMax)
            offset.x = -m_cursorOffset.x - panelSize.x;

        // Flip above the cursor if we'd overflow the bottom edge.
        if (localPoint.y + offset.y - panelSize.y < parentRect.yMin)
            offset.y = -m_cursorOffset.y + panelSize.y;

        Vector2 anchoredPos = localPoint + offset;

        // Final clamp for edge cases where the panel is larger than the remaining space on
        // either side of the cursor. Keeps the whole panel on screen even if the chosen side
        // doesn't actually fit.
        anchoredPos.x = Mathf.Clamp(anchoredPos.x, parentRect.xMin, parentRect.xMax - panelSize.x);
        anchoredPos.y = Mathf.Clamp(anchoredPos.y, parentRect.yMin + panelSize.y, parentRect.yMax);

        m_panel.anchoredPosition = anchoredPos;
    }

    private bool TryGetAnchorScreenPoint(Entry entry, out Vector2 screenPos)
    {
        switch (entry.Mode)
        {
            case AnchorMode.UI:
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
