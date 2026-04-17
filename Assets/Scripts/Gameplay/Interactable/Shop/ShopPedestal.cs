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

    private Lootable m_itemPrefab;
    private LootableRarity.Rarity m_rarity;
    private int m_price;
    private bool m_isSold;
    private ShopRoom m_shopRoom;
    private CreditManager m_creditManager;
    private PlayerInteractionDisplay m_interactionDisplay;

    public void Initialize(Lootable itemPrefab, LootableRarity.Rarity rarity, int price, ShopRoom shopRoom)
    {
        m_itemPrefab = itemPrefab;
        m_rarity = rarity;
        m_price = price;
        m_shopRoom = shopRoom;

        ServiceLocator.Global.TryGet(out m_creditManager);
        ServiceLocator.Global.TryGet(out m_interactionDisplay);

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

        if (m_creditManager != null && m_creditManager.TrySpend(m_price))
        {
            SpawnItem();
            MarkSold();
        }
    }

    private void Steal()
    {
        if (m_isSold || m_itemPrefab == null) return;

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

        m_interactionDisplay?.Hide(this);
        m_inputReader.Interact -= Buy;
        m_inputReader.AltInteract -= Steal;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (m_isSold || other.gameObject.layer != GameConstants.Layers.PlayerLayer) return;

        m_interactionDisplay?.Show(GetInteractText(), this);
        m_inputReader.Interact += Buy;
        m_inputReader.AltInteract += Steal;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer != GameConstants.Layers.PlayerLayer) return;

        m_interactionDisplay?.Hide(this);
        m_inputReader.Interact -= Buy;
        m_inputReader.AltInteract -= Steal;
    }

    private string GetInteractText()
    {
        bool canAfford = m_creditManager != null && m_creditManager.Credits >= m_price;
        return canAfford
            ? "[E] Acquire  [F] Jack"
            : "[F] Jack";
    }

    private void OnDestroy()
    {
        m_inputReader.Interact -= Buy;
        m_inputReader.AltInteract -= Steal;
    }
}
