using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityServiceLocator;

public class Tooltip : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform m_panel;
    [SerializeField] private CanvasGroup m_canvasGroup;
    [SerializeField] private TextMeshProUGUI m_titleText;
    [SerializeField] private TextMeshProUGUI m_bodyText;
    [SerializeField] private TextMeshProUGUI m_effectText;

    [Header("Positioning")]
    [Tooltip("Offset from the cursor in screen pixels. Positive X is to the right of the cursor, positive Y is above it.")]
    [SerializeField] private Vector2 m_cursorOffset = new Vector2(16f, -16f);

    private RectTransform m_parentRect;
    private Canvas m_canvas;
    private bool m_visible;

    private void Awake()
    {
        ServiceLocator.Global.Register(this);

        m_parentRect = m_panel.parent as RectTransform;
        m_canvas = GetComponentInParent<Canvas>();

        HideImmediate();
    }

    public void Show(string title, string body, string effect)
    {
        if (m_titleText != null) m_titleText.text = title;
        if (m_bodyText != null) m_bodyText.text = body;
        if (m_effectText != null) m_effectText.text = effect;

        m_visible = true;
        m_canvasGroup.alpha = 1f;
        m_canvasGroup.blocksRaycasts = false;
        UpdatePosition();
    }

    public void Hide()
    {
        m_visible = false;
        m_canvasGroup.alpha = 0f;
        m_canvasGroup.blocksRaycasts = false;
    }

    private void HideImmediate()
    {
        m_visible = false;
        m_canvasGroup.alpha = 0f;
        m_canvasGroup.blocksRaycasts = false;
    }

    private void LateUpdate()
    {
        if (m_visible) UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (Mouse.current == null || m_parentRect == null) return;

        Vector2 screenPos = Mouse.current.position.ReadValue();
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
}
