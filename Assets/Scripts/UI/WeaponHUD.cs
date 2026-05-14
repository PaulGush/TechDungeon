using System.Text;
using PlayerObject;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityServiceLocator;

public class WeaponHUD : MonoBehaviour
{
    /// <summary>
    /// How the magazine readout behaves while a reload is in progress. Switchable in the
    /// inspector so the look can be A/B-tested at runtime; <see cref="ReloadStyle.Plain"/>
    /// is the original static "RELOADING..." text.
    /// </summary>
    private enum ReloadStyle
    {
        Plain,        // static "RELOADING..."
        IconFill,     // a fill wipe revealing the ammo icon (default: bottom -> top) + the count climbing 0 -> max
        TextBar,      // "RELOAD [====----]" filling left -> right, colour ramping to the ammo colour
        PipRefill,    // "O O O o o o" — one slot per round, lighting up as they load
        CounterSpin   // the ammo number cycles random digits, snapping to the real count on done
    }

    [Header("References")]
    [SerializeField] private WeaponHolder m_weaponHolder;

    [Tooltip("GameObject toggled off when the player is unarmed and back on when they equip a weapon. Leave empty to toggle this HUD's own GameObject.")]
    [SerializeField] private GameObject m_root;

    [Header("Ammo")]
    [SerializeField] private Image m_ammoIcon;

    [Tooltip("Shows 'current / max' between reloads; during a reload it's driven by the Reload Style. Leave empty to skip.")]
    [SerializeField] private TextMeshProUGUI m_magazineText;

    [Header("Reload display")]
    [SerializeField] private ReloadStyle m_reloadStyle = ReloadStyle.IconFill;

    [Tooltip("IconFill style: how the overlay reveals the ammo icon as the reload progresses. Vertical = bottom-to-top.")]
    [SerializeField] private Image.FillMethod m_iconFillMethod = Image.FillMethod.Vertical;

    [Tooltip("Colour the reload visual lerps from (empty) toward the ammo colour (full).")]
    [SerializeField] private Color m_reloadEmptyColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Tooltip("Cell count for the TextBar style. PipRefill uses the magazine size instead, falling back to this when the magazine is huge.")]
    [SerializeField, Range(4, 24)] private int m_barCells = 8;

    [Tooltip("Seconds between digit rolls in the CounterSpin style.")]
    [SerializeField, Min(0.01f)] private float m_spinInterval = 0.05f;

    [Tooltip("Scale punch applied to the magazine text the instant a reload completes. 1 = no punch.")]
    [SerializeField, Min(1f)] private float m_completePunchScale = 1.3f;

    [Tooltip("Seconds the completion punch takes to settle back to normal scale.")]
    [SerializeField, Min(0.01f)] private float m_completePunchDuration = 0.18f;

    [Tooltip("PipRefill only. Scale a pip pops to the instant its round chambers, decaying back to 1. 1 disables the bulge.")]
    [SerializeField, Min(1f)] private float m_pipBulgeScale = 1.6f;

    [Tooltip("PipRefill only. Seconds the pip bulge takes to settle back to base scale.")]
    [SerializeField, Min(0.01f)] private float m_pipBulgeDuration = 0.18f;

    private AmmoManager m_ammoManager;
    private WeaponShooting m_currentShooting;
    private Color m_ammoColor = Color.white;

    private Image m_reloadFill;            // runtime-created Filled-image overlay on the ammo icon

    private bool m_reloading;
    private float m_reloadDuration;
    private float m_reloadElapsed;
    private float m_nextSpinAt;
    private int m_spinShown;
    private System.Random m_spinRng;

    private float m_punchTimeLeft;
    private Vector3 m_textBaseScale = Vector3.one;
    private readonly StringBuilder m_sb = new StringBuilder(64);

    // PerRound bulge tracking: index of the pip whose round was most recently chambered, and
    // when its bulge animation should end. m_lastMagazineCount tracks the previous OnMagazineChanged
    // value so we only bulge on increments (firing decrements shouldn't trigger it).
    private int m_pipBulgeIndex = -1;
    private float m_pipBulgeEndTime;
    private int m_lastMagazineCount = -1;

    // After the last round chambers in PerRound+PipRefill we keep rendering the pip view for
    // one bulge duration so the final pip's pop is visible before the ammo-count text returns.
    // 0 means no tail active.
    private float m_reloadTailEndTime;

    private void Start()
    {
        if (m_magazineText != null) m_textBaseScale = m_magazineText.rectTransform.localScale;
        m_spinRng = new System.Random();

        ServiceLocator.Global.TryGet(out m_ammoManager);
        if (m_ammoManager != null)
        {
            m_ammoManager.OnAmmoChanged += OnAmmoChanged;
            UpdateAmmoDisplay(m_ammoManager.CurrentAmmoSettings);
        }

        if (m_weaponHolder != null)
        {
            m_weaponHolder.OnWeaponChanged += OnWeaponChanged;
            OnWeaponChanged(m_weaponHolder.CurrentWeapon);
        }
        else
        {
            SetRootActive(false);
        }
    }

    private void OnDestroy()
    {
        if (m_ammoManager != null)
            m_ammoManager.OnAmmoChanged -= OnAmmoChanged;
        if (m_weaponHolder != null)
            m_weaponHolder.OnWeaponChanged -= OnWeaponChanged;

        BindShooting(null);
    }

    private void Update()
    {
        if (m_reloading && m_currentShooting != null)
        {
            // Tail: reload coroutine has finished but we're still letting the last pip's bulge
            // play out. When it expires, fall through to the real completion (text + punch).
            if (m_reloadTailEndTime > 0f)
            {
                if (Time.time >= m_reloadTailEndTime)
                {
                    FinalizeReloadCompletion();
                }
                else
                {
                    RenderReloadState(1f);
                }
            }
            else
            {
                m_reloadElapsed += Time.deltaTime;
                float progress = m_reloadDuration > 0f ? Mathf.Clamp01(m_reloadElapsed / m_reloadDuration) : 1f;
                RenderReloadState(progress);
            }
        }

        if (m_punchTimeLeft > 0f && m_magazineText != null)
        {
            m_punchTimeLeft -= Time.deltaTime;
            float t = m_completePunchDuration > 0f ? Mathf.Clamp01(m_punchTimeLeft / m_completePunchDuration) : 0f;
            m_magazineText.rectTransform.localScale = Vector3.LerpUnclamped(m_textBaseScale, m_textBaseScale * m_completePunchScale, t);
            if (m_punchTimeLeft <= 0f)
                m_magazineText.rectTransform.localScale = m_textBaseScale;
        }
    }

    private void OnAmmoChanged(AmmoSettings settings) => UpdateAmmoDisplay(settings);

    private void OnWeaponChanged(GameObject weapon)
    {
        SetRootActive(weapon != null);
        BindShooting(weapon != null ? weapon.GetComponent<WeaponShooting>() : null);
    }

    private void SetRootActive(bool active)
    {
        GameObject root = m_root != null ? m_root : gameObject;
        if (root.activeSelf != active)
            root.SetActive(active);
    }

    private void BindShooting(WeaponShooting shooting)
    {
        if (m_currentShooting != null)
        {
            m_currentShooting.OnMagazineChanged -= OnMagazineChanged;
            m_currentShooting.OnReloadStarted -= OnReloadStarted;
            m_currentShooting.OnReloadCompleted -= OnReloadCompleted;
            m_currentShooting.OnReloadCancelled -= OnReloadCancelled;
        }

        EndReload();
        m_punchTimeLeft = 0f;
        if (m_magazineText != null) m_magazineText.rectTransform.localScale = m_textBaseScale;
        m_pipBulgeIndex = -1;
        m_pipBulgeEndTime = 0f;
        m_lastMagazineCount = shooting != null ? shooting.MagazineCurrent : -1;

        m_currentShooting = shooting;

        if (m_currentShooting != null)
        {
            m_currentShooting.OnMagazineChanged += OnMagazineChanged;
            m_currentShooting.OnReloadStarted += OnReloadStarted;
            m_currentShooting.OnReloadCompleted += OnReloadCompleted;
            m_currentShooting.OnReloadCancelled += OnReloadCancelled;
            RefreshMagazineDisplay();
        }
        else if (m_magazineText != null)
        {
            m_magazineText.text = string.Empty;
        }
    }

    private void OnMagazineChanged(int current, int max)
    {
        bool perRound = m_currentShooting != null && m_currentShooting.ReloadStyle == WeaponReloadStyle.PerRound;
        if (perRound && current > m_lastMagazineCount && current > 0)
        {
            m_pipBulgeIndex = current - 1;
            m_pipBulgeEndTime = Time.time + m_pipBulgeDuration;
        }
        m_lastMagazineCount = current;

        if (!m_reloading) RefreshMagazineDisplay();
    }

    private void OnReloadStarted(float duration)
    {
        m_reloading = true;
        m_reloadDuration = duration;
        m_reloadElapsed = 0f;
        m_nextSpinAt = 0f;
        m_spinShown = 0;

        if (m_reloadStyle == ReloadStyle.IconFill)
        {
            EnsureReloadFill();
            if (m_reloadFill != null)
            {
                m_reloadFill.sprite = m_ammoIcon != null ? m_ammoIcon.sprite : null;
                m_reloadFill.fillMethod = m_iconFillMethod;
                m_reloadFill.fillOrigin = DefaultFillOrigin(m_iconFillMethod);
                m_reloadFill.fillClockwise = true;
                m_reloadFill.fillAmount = 0f;
                m_reloadFill.gameObject.SetActive(m_reloadFill.sprite != null);
            }
        }

        RenderReloadState(0f);
    }

    private void OnReloadCompleted()
    {
        bool needsBulgeTail = m_reloadStyle == ReloadStyle.PipRefill
            && m_currentShooting != null
            && m_currentShooting.ReloadStyle == WeaponReloadStyle.PerRound
            && m_pipBulgeScale > 1f && m_pipBulgeDuration > 0f;

        if (needsBulgeTail)
        {
            // Hold the pip view for one bulge duration so the last pip's pop is visible. Update()
            // calls FinalizeReloadCompletion when the tail expires.
            m_reloadTailEndTime = Time.time + m_pipBulgeDuration;
        }
        else
        {
            FinalizeReloadCompletion();
        }
    }

    private void FinalizeReloadCompletion()
    {
        m_reloadTailEndTime = 0f;
        EndReload();
        RefreshMagazineDisplay();
        StartCompletePunch();
    }

    private void OnReloadCancelled()
    {
        EndReload();
        RefreshMagazineDisplay();
    }

    private void EndReload()
    {
        m_reloading = false;
        m_reloadTailEndTime = 0f;
        if (m_reloadFill != null) m_reloadFill.gameObject.SetActive(false);
    }

    private void StartCompletePunch()
    {
        if (m_magazineText == null || m_completePunchScale <= 1f || m_completePunchDuration <= 0f) return;
        m_punchTimeLeft = m_completePunchDuration;
        m_magazineText.rectTransform.localScale = m_textBaseScale * m_completePunchScale;
    }

    private void RefreshMagazineDisplay()
    {
        if (m_magazineText == null || m_currentShooting == null) return;
        m_magazineText.text = m_currentShooting.UsesMagazine
            ? $"{m_currentShooting.MagazineCurrent} / {m_currentShooting.MagazineMax}"
            : string.Empty;
    }

    private void RenderReloadState(float progress)
    {
        if (m_magazineText == null) return;
        int max = m_currentShooting != null ? m_currentShooting.MagazineMax : 0;

        switch (m_reloadStyle)
        {
            case ReloadStyle.Plain:
                m_magazineText.text = "RELOADING...";
                break;

            case ReloadStyle.IconFill:
            {
                if (m_reloadFill != null && m_reloadFill.gameObject.activeSelf)
                {
                    m_reloadFill.fillAmount = progress;
                    m_reloadFill.color = Color.Lerp(m_reloadEmptyColor, m_ammoColor, progress);
                }
                int shown = Mathf.Clamp(Mathf.FloorToInt(progress * max), 0, max);
                m_magazineText.text = $"{shown} / {max}";
                break;
            }

            case ReloadStyle.TextBar:
            {
                int cells = Mathf.Clamp(m_barCells, 4, 24);
                int filled = Mathf.Clamp(Mathf.RoundToInt(progress * cells), 0, cells);
                m_sb.Clear();
                m_sb.Append("RELOAD [");
                AppendColored(m_sb, '=', filled, Color.Lerp(m_reloadEmptyColor, m_ammoColor, progress));
                AppendColored(m_sb, '-', cells - filled, m_reloadEmptyColor);
                m_sb.Append(']');
                m_magazineText.text = m_sb.ToString();
                break;
            }

            case ReloadStyle.PipRefill:
            {
                int pips = (max > 0 && max <= 24) ? max : Mathf.Clamp(m_barCells, 4, 24);
                bool perRound = m_currentShooting != null && m_currentShooting.ReloadStyle == WeaponReloadStyle.PerRound;

                // Solid pips = rounds actually chambered. In PerRound mode the magazine fills one
                // round at a time as OnMagazineChanged fires, so pips light up as each round is
                // loaded. In Full mode the progress bar drives the same display, sweeping pips on
                // in lockstep with the timer.
                int filled = perRound
                    ? Mathf.Clamp(m_currentShooting.MagazineCurrent, 0, pips)
                    : Mathf.Clamp(Mathf.FloorToInt(progress * pips), 0, pips);

                string onHex = ColorUtility.ToHtmlStringRGB(m_ammoColor);
                string offHex = ColorUtility.ToHtmlStringRGB(m_reloadEmptyColor);
                string partialHex = perRound && filled < pips
                    ? ColorUtility.ToHtmlStringRGB(Color.Lerp(m_reloadEmptyColor, m_ammoColor, progress))
                    : null;

                m_sb.Clear();
                for (int i = 0; i < pips; i++)
                {
                    if (i > 0) m_sb.Append(' ');
                    if (i < filled)
                    {
                        m_sb.Append("<color=#").Append(onHex).Append(">O</color>");
                    }
                    else if (partialHex != null && i == filled)
                    {
                        m_sb.Append("<color=#").Append(partialHex).Append(">o</color>");
                    }
                    else
                    {
                        m_sb.Append("<color=#").Append(offHex).Append(">o</color>");
                    }
                }
                m_magazineText.text = m_sb.ToString();

                // Bulge: scale the freshly-chambered pip's mesh quad around its centre, after the
                // text is built. Doing this via vertex manipulation rather than a TMP <size> tag
                // avoids the line-height changes that would otherwise bounce the whole pip line.
                if (perRound && m_pipBulgeScale > 1f && Time.time < m_pipBulgeEndTime
                    && m_pipBulgeIndex >= 0 && m_pipBulgeIndex < pips)
                {
                    float t = Mathf.Clamp01((m_pipBulgeEndTime - Time.time) / m_pipBulgeDuration);
                    ApplyPipBulgeMesh(m_pipBulgeIndex, Mathf.Lerp(1f, m_pipBulgeScale, t));
                }
                break;
            }

            case ReloadStyle.CounterSpin:
            {
                if (m_reloadElapsed >= m_nextSpinAt)
                {
                    m_nextSpinAt += Mathf.Max(0.01f, m_spinInterval);
                    m_spinShown = max > 0 ? m_spinRng.Next(0, max + 1) : m_spinRng.Next(0, 100);
                }
                m_magazineText.text = $"{m_spinShown} / {max}";
                break;
            }
        }
    }

    // Scales pip #pipIndex's character quad around its centre by `scale`. Works on the already-set
    // text — call after assigning m_magazineText.text. ForceMeshUpdate gives us up-to-date
    // CharacterInfo; we walk the visible characters (the ' ' separators are non-visible) to find
    // the one whose vertices we need to push outward, then push the modified meshInfo array back
    // to the live mesh (UpdateGeometry only recalculates bounds + re-binds, it doesn't copy
    // vertices from meshInfo to mesh on its own — see TMP's VertexJitter sample).
    private void ApplyPipBulgeMesh(int pipIndex, float scale)
    {
        if (m_magazineText == null) return;
        m_magazineText.ForceMeshUpdate();

        var info = m_magazineText.textInfo;
        int seen = 0;
        int targetChar = -1;
        for (int i = 0; i < info.characterCount; i++)
        {
            if (!info.characterInfo[i].isVisible) continue;
            if (seen == pipIndex) { targetChar = i; break; }
            seen++;
        }
        if (targetChar < 0) return;

        var ci = info.characterInfo[targetChar];
        int matIdx = ci.materialReferenceIndex;
        int vertIdx = ci.vertexIndex;
        var meshInfo = info.meshInfo[matIdx];
        var verts = meshInfo.vertices;

        Vector3 centre = (verts[vertIdx + 0] + verts[vertIdx + 2]) * 0.5f;
        for (int v = 0; v < 4; v++)
            verts[vertIdx + v] = centre + (verts[vertIdx + v] - centre) * scale;

        meshInfo.mesh.vertices = verts;
        m_magazineText.UpdateGeometry(meshInfo.mesh, matIdx);
    }

    private static void AppendColored(StringBuilder sb, char c, int count, Color color)
    {
        if (count <= 0) return;
        sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(color)).Append('>');
        for (int i = 0; i < count; i++) sb.Append(c);
        sb.Append("</color>");
    }

    private void EnsureReloadFill()
    {
        if (m_reloadFill != null || m_ammoIcon == null) return;

        var go = new GameObject("ReloadFill");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(m_ammoIcon.rectTransform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;

        m_reloadFill = go.AddComponent<Image>();
        m_reloadFill.raycastTarget = false;
        m_reloadFill.preserveAspect = true;
        m_reloadFill.type = Image.Type.Filled;
        m_reloadFill.fillMethod = m_iconFillMethod;
        m_reloadFill.fillOrigin = DefaultFillOrigin(m_iconFillMethod);
        m_reloadFill.fillClockwise = true;
        m_reloadFill.fillAmount = 0f;
        go.SetActive(false);
    }

    private static int DefaultFillOrigin(Image.FillMethod method) => method switch
    {
        Image.FillMethod.Horizontal => (int)Image.OriginHorizontal.Left,
        Image.FillMethod.Vertical => (int)Image.OriginVertical.Bottom,
        Image.FillMethod.Radial90 => (int)Image.Origin90.BottomLeft,
        Image.FillMethod.Radial180 => (int)Image.Origin180.Bottom,
        Image.FillMethod.Radial360 => (int)Image.Origin360.Bottom,
        _ => 0,
    };

    private void UpdateAmmoDisplay(AmmoSettings settings)
    {
        if (settings == null) return;
        m_ammoColor = settings.ProjectileColor;

        if (m_ammoIcon != null)
        {
            m_ammoIcon.color = settings.ProjectileColor;
            if (settings.Icon != null)
            {
                m_ammoIcon.sprite = settings.Icon;
                m_ammoIcon.enabled = true;
            }
            else
            {
                m_ammoIcon.enabled = false;
            }
        }
    }
}
