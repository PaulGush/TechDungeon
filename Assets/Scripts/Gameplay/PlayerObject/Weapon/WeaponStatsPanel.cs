using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A toggleable on-screen panel that presents a weapon's stats as labelled fill bars
/// (Power / Rate of Fire / Stability / Reload Speed / Precision / Ammo Capacity), RE-Requiem style.
///
/// Two ways to set this up:
///   1. <b>Prefab</b> (preferred when you want layout control): build the UI by hand in a prefab,
///      assign the references in the "Prefab Wiring" section below, and the procedural Build is
///      skipped. <see cref="WeaponHolder"/> / <see cref="ShopPedestal"/> instantiate the prefab.
///   2. <b>Procedural</b> (zero-setup fallback): leave the prefab-wiring fields null and the panel
///      builds itself from the "Look" fields below at runtime. Used when no prefab is assigned to
///      the consumer.
///
/// Bars/value-texts must be supplied in the same order as <see cref="s_labels"/>.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class WeaponStatsPanel : MonoBehaviour
{
    private static readonly string[] s_labels =
        { "Power", "Rate of Fire", "Stability", "Reload Speed", "Precision", "Ammo Capacity" };

    [Header("Normalisation — the value that fills a bar to 100%")]
    [SerializeField] private float m_powerMax = 60f;          // base projectile damage × weapon damage multiplier
    [SerializeField] private float m_rateOfFireMax = 15f;     // shots per second
    [SerializeField] private float m_recoilMaxDeg = 20f;      // per-shot recoil at which Stability bottoms out
    [SerializeField] private float m_reloadMaxSeconds = 4f;   // reload time at which Reload Speed bottoms out
    [SerializeField] private float m_spreadMaxDeg = 12f;      // spread at which Precision bottoms out
    [SerializeField] private float m_magazineMax = 30f;       // magazine size that fills the Ammo Capacity bar

    [Header("Prefab Wiring (optional — leave empty for procedural build)")]
    [Tooltip("Root rect to enable/disable. If null, this GameObject is used.")]
    [SerializeField] private RectTransform m_panelSerialized;
    [SerializeField] private TextMeshProUGUI m_titleSerialized;
    [Tooltip("Six bar Fill rects, in s_labels order. Their anchorMax.x is driven 0..1 from each stat.")]
    [SerializeField] private RectTransform[] m_fillSerialized;
    [Tooltip("Six bar Fill Images, in s_labels order. Tinted neutral/better/worse based on comparison.")]
    [SerializeField] private Image[] m_fillImageSerialized;
    [Tooltip("Six row value labels, in s_labels order. Only Ammo Capacity sets text; the rest are blanked.")]
    [SerializeField] private TextMeshProUGUI[] m_valueSerialized;

    [Header("Look (procedural fallback only)")]
    [SerializeField] private TMP_FontAsset m_font;
    [SerializeField] private Vector2 m_anchoredPosition = new Vector2(0f, 130f);
    [SerializeField] private float m_panelWidth = 380f;
    [SerializeField] private float m_labelWidth = 130f;
    [SerializeField] private float m_valueWidth = 48f;
    [SerializeField] private float m_rowHeight = 22f;
    [SerializeField] private int m_fontSize = 16;
    [SerializeField] private int m_titleFontSize = 20;
    [SerializeField] private int m_sortingOrder = 200;
    [SerializeField] private Color m_panelColor = new Color(0.06f, 0.07f, 0.09f, 0.9f);
    [SerializeField] private Color m_barBackColor = new Color(0.18f, 0.2f, 0.24f, 1f);
    [Tooltip("Bar colour when there's no held weapon to compare against, or the stat is the same.")]
    [SerializeField] private Color m_barNeutralColor = new Color(0.92f, 0.94f, 0.97f, 1f);
    [Tooltip("Bar colour when the floor weapon's stat beats the held weapon's.")]
    [SerializeField] private Color m_barBetterColor = new Color(0.37f, 0.86f, 0.42f, 1f);
    [Tooltip("Bar colour when the floor weapon's stat is worse than the held weapon's.")]
    [SerializeField] private Color m_barWorseColor = new Color(0.9f, 0.36f, 0.36f, 1f);
    [SerializeField] private Color m_titleColor = Color.white;
    [SerializeField] private Color m_labelColor = new Color(0.78f, 0.81f, 0.85f, 1f);
    [SerializeField] private Color m_valueColor = Color.white;

    private RectTransform m_panel;
    private TextMeshProUGUI m_title;
    private RectTransform[] m_fill;
    private Image[] m_fillImage;
    private TextMeshProUGUI[] m_value;
    private bool m_built;

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only: run the procedural Build now (in the prefab editor) and copy the resulting
    /// references into the serialized fields. After baking the layout becomes plain child
    /// GameObjects on this prefab that the user can rearrange, restyle, or replace; the
    /// serialized references mean the Build() short-circuit will keep using them.
    /// </summary>
    [ContextMenu("Bake Procedural Layout")]
    private void BakeProceduralLayout()
    {
        // Strip any existing children so re-baking is idempotent.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            UnityEditor.Undo.DestroyObjectImmediate(transform.GetChild(i).gameObject);
        }
        m_panelSerialized = null;
        m_titleSerialized = null;
        m_fillSerialized = null;
        m_fillImageSerialized = null;
        m_valueSerialized = null;
        m_built = false;

        Build();

        m_panelSerialized = m_panel;
        m_titleSerialized = m_title;
        m_fillSerialized = m_fill;
        m_fillImageSerialized = m_fillImage;
        m_valueSerialized = m_value;

        // Build() set Canvas.renderMode directly — mark the Canvas dirty too or the change is
        // lost on save. (Direct property writes don't trip the editor's modification tracking.)
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null) UnityEditor.EditorUtility.SetDirty(canvas);

        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.EditorUtility.SetDirty(gameObject);
    }
#endif

    private void Awake()
    {
        Build();
        SetVisible(false);
    }

    /// <summary>
    /// Show the panel for an in-world weapon, reading both weapons' rarity from their Lootable.
    /// </summary>
    public void Show(WeaponShooting floorWeapon, WeaponShooting heldWeapon)
        => Show(floorWeapon, RarityOf(floorWeapon), heldWeapon, RarityOf(heldWeapon));

    /// <summary>
    /// Populate the bars from <paramref name="floorWeapon"/> (at <paramref name="floorRarity"/>) and
    /// show the panel. If <paramref name="heldWeapon"/> is non-null each bar is tinted green/red/white
    /// depending on whether the floor weapon's stat is better/worse/equal — every bar is built so
    /// "more = better", so the comparison is uniform (e.g. Reload Speed is the inverse of reload time).
    /// The explicit rarities let the shop preview a weapon at the rarity it will be sold at, rather
    /// than the prefab's authored rarity.
    /// </summary>
    public void Show(WeaponShooting floorWeapon, LootableRarity.Rarity floorRarity, WeaponShooting heldWeapon, LootableRarity.Rarity heldRarity)
    {
        if (!m_built) Build();
        if (floorWeapon == null || floorWeapon.Settings == null) { SetVisible(false); return; }

        m_title.text = string.IsNullOrEmpty(floorWeapon.DisplayName) ? "Weapon" : floorWeapon.DisplayName;

        var floor = ComputeStats(floorWeapon, floorRarity);
        bool hasComparison = heldWeapon != null && heldWeapon.Settings != null;
        float[] held = hasComparison ? ComputeStats(heldWeapon, heldRarity).compare : null;

        const float eps = 0.0001f;
        for (int i = 0; i < s_labels.Length; i++)
        {
            m_fill[i].anchorMax = new Vector2(Mathf.Clamp01(floor.compare[i]), 1f);
            m_value[i].text = floor.valueText[i];

            Color c = m_barNeutralColor;
            if (hasComparison)
            {
                if (floor.compare[i] > held[i] + eps) c = m_barBetterColor;
                else if (floor.compare[i] < held[i] - eps) c = m_barWorseColor;
            }
            m_fillImage[i].color = c;
        }

        SetVisible(true);
    }

    public void Hide() => SetVisible(false);

    public bool IsShowing => m_panel != null && m_panel.gameObject.activeSelf;

    private static LootableRarity.Rarity RarityOf(WeaponShooting weapon)
        => weapon != null && weapon.TryGetComponent(out Lootable l) ? l.Rarity : LootableRarity.Rarity.Common;

    // Per-stat "goodness" (higher is always better; not yet clamped — fill = Clamp01 of these) plus
    // the optional value-text shown on a row (only Ammo Capacity uses it).
    private (float[] compare, string[] valueText) ComputeStats(WeaponShooting weapon, LootableRarity.Rarity rarity)
    {
        var compare = new float[s_labels.Length];
        var valueText = new[] { "", "", "", "", "", "" };
        WeaponSettings s = weapon != null ? weapon.Settings : null;
        if (s == null) return (compare, valueText);

        int baseDamage = weapon.ProjectilePrefab != null ? weapon.ProjectilePrefab.BaseDamage : 0;
        // Fold in the weapon's rarity damage bonus so a higher-rarity copy of the same gun reads as
        // hitting harder (matches what WeaponShooting actually applies at fire time).
        float power = baseDamage * s.DamageMultiplier * LootableRarity.GetDamageMultiplier(rarity);

        float shotsPerSecond;
        switch (s.FireMode)
        {
            case WeaponFireMode.Burst:
                shotsPerSecond = Mathf.Max(1, s.BurstCount) / Mathf.Max(0.01f, s.Cooldown);
                break;
            case WeaponFireMode.Charge:
                shotsPerSecond = 1f / Mathf.Max(0.01f, s.Cooldown + s.MaxChargeSeconds);
                break;
            default:
                shotsPerSecond = 1f / Mathf.Max(0.01f, s.Cooldown);
                break;
        }

        bool hasMag = s.MagazineSize > 0;
        compare[0] = power / Mathf.Max(1f, m_powerMax);                                  // Power
        compare[1] = shotsPerSecond / Mathf.Max(0.01f, m_rateOfFireMax);                 // Rate of Fire
        compare[2] = 1f - s.RecoilDegrees / Mathf.Max(0.01f, m_recoilMaxDeg);            // Stability  (less recoil = better)
        compare[3] = hasMag ? 1f - s.ReloadDuration / Mathf.Max(0.01f, m_reloadMaxSeconds) : 2f; // Reload Speed (faster = better; no-mag = best)
        compare[4] = 1f - s.SpreadDegrees / Mathf.Max(0.01f, m_spreadMaxDeg);            // Precision (less spread = better)
        compare[5] = hasMag ? s.MagazineSize / Mathf.Max(1f, m_magazineMax) : 2f;        // Ammo Capacity (more = better; no-mag = "infinite")
        valueText[5] = hasMag ? s.MagazineSize.ToString() : "∞";
        return (compare, valueText);
    }

    private void SetVisible(bool on)
    {
        if (m_panel != null) m_panel.gameObject.SetActive(on);
    }

    private void Build()
    {
        if (m_built) return;
        m_built = true;

        // Prefab path: references already wired in the inspector — skip procedural construction.
        if (m_titleSerialized != null
            && m_fillSerialized != null && m_fillSerialized.Length == s_labels.Length
            && m_fillImageSerialized != null && m_fillImageSerialized.Length == s_labels.Length
            && m_valueSerialized != null && m_valueSerialized.Length == s_labels.Length)
        {
            m_panel = m_panelSerialized != null ? m_panelSerialized : (RectTransform)transform;
            m_title = m_titleSerialized;
            m_fill = m_fillSerialized;
            m_fillImage = m_fillImageSerialized;
            m_value = m_valueSerialized;
            return;
        }

        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = m_sortingOrder;
        if (GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // Panel: bottom-centre, fixed width, height auto-fits the rows.
        m_panel = NewRect("Panel", transform);
        m_panel.anchorMin = m_panel.anchorMax = new Vector2(0.5f, 0f);
        m_panel.pivot = new Vector2(0.5f, 0f);
        m_panel.anchoredPosition = m_anchoredPosition;
        m_panel.sizeDelta = new Vector2(m_panelWidth, 0f);
        m_panel.gameObject.AddComponent<Image>().color = m_panelColor;

        VerticalLayoutGroup vlg = m_panel.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 14, 12, 12);
        vlg.spacing = 5f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter fitter = m_panel.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        m_title = NewText("Title", m_panel, m_titleFontSize, m_titleColor, TextAlignmentOptions.Center);
        AddLayoutElement(m_title.gameObject, preferredHeight: m_titleFontSize + 10f);

        int n = s_labels.Length;
        m_fill = new RectTransform[n];
        m_fillImage = new Image[n];
        m_value = new TextMeshProUGUI[n];
        for (int i = 0; i < n; i++)
        {
            RectTransform row = NewRect("Row_" + s_labels[i].Replace(' ', '_'), m_panel);
            AddLayoutElement(row.gameObject, preferredHeight: m_rowHeight);
            HorizontalLayoutGroup hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            TextMeshProUGUI label = NewText("Label", row, m_fontSize, m_labelColor, TextAlignmentOptions.MidlineLeft);
            label.text = s_labels[i];
            AddLayoutElement(label.gameObject, preferredWidth: m_labelWidth, flexibleWidth: 0f);

            RectTransform barBg = NewRect("Bar", row);
            barBg.gameObject.AddComponent<Image>().color = m_barBackColor;
            AddLayoutElement(barBg.gameObject, flexibleWidth: 1f);

            RectTransform fill = NewRect("Fill", barBg);
            Image fillImg = fill.gameObject.AddComponent<Image>();
            fillImg.color = m_barNeutralColor;
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(1f, 1f);
            fill.offsetMin = new Vector2(0f, 2f);
            fill.offsetMax = new Vector2(0f, -2f);
            m_fill[i] = fill;
            m_fillImage[i] = fillImg;

            TextMeshProUGUI value = NewText("Value", row, m_fontSize, m_valueColor, TextAlignmentOptions.MidlineRight);
            AddLayoutElement(value.gameObject, preferredWidth: m_valueWidth, flexibleWidth: 0f);
            m_value[i] = value;
        }
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    private TextMeshProUGUI NewText(string name, Transform parent, float fontSize, Color color, TextAlignmentOptions align)
    {
        RectTransform rt = NewRect(name, parent);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (m_font != null) tmp.font = m_font;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void AddLayoutElement(GameObject go, float preferredWidth = -1f, float preferredHeight = -1f, float flexibleWidth = -1f)
    {
        var le = go.AddComponent<LayoutElement>();
        if (preferredWidth >= 0f) le.preferredWidth = preferredWidth;
        if (preferredHeight >= 0f) le.preferredHeight = preferredHeight;
        if (flexibleWidth >= 0f) le.flexibleWidth = flexibleWidth;
    }
}
