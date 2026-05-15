using Input;
using PlayerObject;
using TMPro;
using UnityEngine;
using UnityServiceLocator;

public class ShopPedestal : MonoBehaviour
{
    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int PixelSizeId = Shader.PropertyToID("_PixelSize");

    [Header("References")]
    [SerializeField] private InputReader m_inputReader;
    [SerializeField] private SpriteRenderer m_itemDisplay;
    [SerializeField] private Transform m_itemSpawnPoint;
    [SerializeField] private TextMeshPro m_priceText;
    [Tooltip("Optional. If set, the comparison stats panel is instantiated from this prefab so its look can be configured in the prefab. If null, a procedural panel is built at runtime.")]
    [SerializeField] private WeaponStatsPanel m_statsPanelPrefab;

    private Lootable m_itemPrefab;
    private LootableRarity.Rarity m_rarity;
    private int m_price;
    private bool m_isSold;
    private ShopRoom m_shopRoom;
    private CreditManager m_creditManager;
    private PlayerInteractionDisplay m_interactionDisplay;
    private Tooltip m_tooltip;
    private string m_tooltipTitle;
    private string m_tooltipBody;
    private string m_tooltipEffect;
    private bool m_hasTooltip;
    private bool m_tooltipShown;

    // Non-null when this pedestal sells a weapon — drives the comparison stat panel.
    private WeaponShooting m_weaponTemplate;
    private WeaponStatsPanel m_statsPanel;
    private bool m_playerInside;

    public void Initialize(Lootable itemPrefab, LootableRarity.Rarity rarity, int price, ShopRoom shopRoom)
    {
        m_itemPrefab = itemPrefab;
        m_rarity = rarity;
        m_price = price;
        m_shopRoom = shopRoom;

        ServiceLocator.Global.TryGet(out m_creditManager);
        ServiceLocator.Global.TryGet(out m_interactionDisplay);
        ServiceLocator.Global.TryGet(out m_tooltip);
        PrepareTooltipContent(itemPrefab);

        if (m_itemDisplay != null)
        {
            SpriteRenderer prefabRenderer = itemPrefab.GetComponentInChildren<SpriteRenderer>();
            if (prefabRenderer != null)
            {
                m_itemDisplay.sprite = prefabRenderer.sprite;
            }

            Material mat = m_itemDisplay.material;
            mat.SetColor(OutlineColorId, LootableRarity.RarityColors[m_rarity]);

            Texture tex = m_itemDisplay.sprite != null ? m_itemDisplay.sprite.texture : null;
            if (tex != null)
                mat.SetVector(PixelSizeId, new Vector4(1f / tex.width, 1f / tex.height, tex.width, tex.height));
        }

        if (m_priceText != null)
        {
            m_priceText.text = $"{m_price} CR";
            m_priceText.enabled = true;
        }
    }

    private void Buy()
    {
        if (m_isSold || m_itemPrefab == null) return;
        if (m_interactionDisplay != null && m_interactionDisplay.CurrentSource != this) return;

        if (m_creditManager != null && m_creditManager.TrySpend(m_price))
        {
            SpawnItem();
            MarkSold();
        }
    }

    private void Steal()
    {
        if (m_isSold || m_itemPrefab == null) return;
        if (m_interactionDisplay != null && m_interactionDisplay.CurrentSource != this) return;

        SpawnItem();
        MarkSold();
        m_shopRoom.HandleSteal(this);
    }

    private void SpawnItem()
    {
        Vector3 spawnPos = m_itemSpawnPoint != null ? m_itemSpawnPoint.position : transform.position;
        Transform roomParent = null;

        if (ServiceLocator.Global.TryGet(out RoomManager roomManager))
        {
            roomParent = roomManager.CurrentRoomTransform;
        }

        GameObject item = Instantiate(m_itemPrefab.gameObject, spawnPos, Quaternion.identity, roomParent);
        Lootable lootable = item.GetComponent<Lootable>();
        if (lootable != null)
        {
            lootable.ChangeRarity(m_rarity);
        }
    }

    private void MarkSold()
    {
        m_isSold = true;

        if (m_itemDisplay != null)
            m_itemDisplay.enabled = false;

        if (m_priceText != null)
            m_priceText.enabled = false;

        m_playerInside = false;
        m_interactionDisplay?.Hide(this);
        HideTooltip();
        PickupDetailsPreference.Changed -= RefreshWeaponStats;
        m_statsPanel?.Hide();
        m_inputReader.Interact -= Buy;
        m_inputReader.AltInteract -= Steal;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (m_isSold || other.gameObject.layer != GameConstants.Layers.PlayerLayer) return;

        m_playerInside = true;
        m_interactionDisplay?.Show(GetInteractText(), this);
        ShowTooltip();
        if (m_weaponTemplate != null)
        {
            PickupDetailsPreference.Changed += RefreshWeaponStats;
            RefreshWeaponStats();
        }
        m_inputReader.Interact += Buy;
        m_inputReader.AltInteract += Steal;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer != GameConstants.Layers.PlayerLayer) return;

        m_playerInside = false;
        m_interactionDisplay?.Hide(this);
        HideTooltip();
        PickupDetailsPreference.Changed -= RefreshWeaponStats;
        m_statsPanel?.Hide();
        m_inputReader.Interact -= Buy;
        m_inputReader.AltInteract -= Steal;
    }

    private void PrepareTooltipContent(Lootable itemPrefab)
    {
        m_weaponTemplate = itemPrefab.GetComponentInChildren<WeaponShooting>();

        IPickupTooltip tooltipSource = itemPrefab.GetComponentInChildren<IPickupTooltip>();
        if (tooltipSource != null && tooltipSource.TryGetTooltip(out m_tooltipTitle, out m_tooltipBody, out m_tooltipEffect))
        {
            m_hasTooltip = true;
            return;
        }

        // Weapons carry no IPickupTooltip — synthesise a minimal one (the stat panel carries the rest).
        if (m_weaponTemplate != null)
        {
            string name = m_weaponTemplate.DisplayName;
            m_tooltipTitle = string.IsNullOrWhiteSpace(name) ? itemPrefab.name : name;
            m_tooltipBody = "Weapon.";
            m_tooltipEffect = string.Empty;
            m_hasTooltip = true;
        }
    }

    // Show the comparison stat panel iff this pedestal sells a weapon, the player is on it (and it's
    // unsold), and the "show details" preference is on; otherwise hide it. Called on enter/exit and
    // when the preference toggles.
    private void RefreshWeaponStats()
    {
        if (m_weaponTemplate != null && m_playerInside && !m_isSold && PickupDetailsPreference.ShowDetails)
        {
            if (m_statsPanel == null)
            {
                if (m_statsPanelPrefab != null)
                {
                    m_statsPanel = Instantiate(m_statsPanelPrefab);
                    m_statsPanel.gameObject.name = "ShopWeaponStatsPanel";
                }
                else
                {
                    var go = new GameObject("ShopWeaponStatsPanel", typeof(RectTransform), typeof(Canvas), typeof(WeaponStatsPanel));
                    m_statsPanel = go.GetComponent<WeaponStatsPanel>();
                }
            }

            WeaponShooting held = null;
            LootableRarity.Rarity heldRarity = LootableRarity.Rarity.Common;
            if (ServiceLocator.Global.TryGet(out WeaponHolder weaponHolder) && weaponHolder.CurrentWeapon != null)
            {
                held = weaponHolder.CurrentWeapon.GetComponent<WeaponShooting>();
                if (weaponHolder.CurrentWeapon.TryGetComponent(out Lootable l)) heldRarity = l.Rarity;
            }
            m_statsPanel.Show(m_weaponTemplate, m_rarity, held, heldRarity);
        }
        else
        {
            m_statsPanel?.Hide();
        }

        // Refresh the prompt suffix too — toggling the preference flips "details" ↔ "hide".
        if (m_playerInside && !m_isSold)
        {
            m_interactionDisplay?.Show(GetInteractText(), this);
        }
    }

    private void ShowTooltip()
    {
        if (!m_hasTooltip || m_tooltip == null) return;
        m_tooltip.Show(m_tooltipTitle, m_tooltipBody, m_tooltipEffect, transform, this);
        m_tooltipShown = true;
    }

    private void HideTooltip()
    {
        if (!m_tooltipShown || m_tooltip == null) return;
        m_tooltip.Hide(this);
        m_tooltipShown = false;
    }

    private string GetInteractText()
    {
        bool canAfford = m_creditManager != null && m_creditManager.Credits >= m_price;
        string text = canAfford
            ? "[Interact] Acquire  [AltInteract] Jack"
            : "[AltInteract] Jack";
        if (m_weaponTemplate != null)
        {
            text += PickupDetailsPreference.ShowDetails ? "   [Sprint] hide" : "   [Sprint] details";
        }
        return text;
    }

    private void OnDestroy()
    {
        m_inputReader.Interact -= Buy;
        m_inputReader.AltInteract -= Steal;
        PickupDetailsPreference.Changed -= RefreshWeaponStats;
        if (m_statsPanel != null) Destroy(m_statsPanel.gameObject);
    }
}
