using Input;
using TMPro;
using UnityEngine;
using UnityServiceLocator;

public class ShopPedestal : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader m_inputReader;
    [SerializeField] private TextMeshPro m_interactText;
    [SerializeField] private SpriteRenderer m_itemDisplay;
    [SerializeField] private Transform m_itemSpawnPoint;

    private Lootable m_itemPrefab;
    private LootableRarity.Rarity m_rarity;
    private int m_price;
    private bool m_isSold;
    private ShopRoom m_shopRoom;
    private CreditManager m_creditManager;

    public void Initialize(Lootable itemPrefab, LootableRarity.Rarity rarity, int price, ShopRoom shopRoom)
    {
        m_itemPrefab = itemPrefab;
        m_rarity = rarity;
        m_price = price;
        m_shopRoom = shopRoom;

        ServiceLocator.Global.TryGet(out m_creditManager);

        if (m_itemDisplay != null)
        {
            SpriteRenderer prefabRenderer = itemPrefab.GetComponentInChildren<SpriteRenderer>();
            if (prefabRenderer != null)
            {
                m_itemDisplay.sprite = prefabRenderer.sprite;
            }
        }

        m_interactText.enabled = false;
    }

    private void Interact()
    {
        if (m_isSold || m_itemPrefab == null) return;

        if (m_creditManager != null && m_creditManager.TrySpend(m_price))
        {
            SpawnItem();
            MarkSold();
        }
        else
        {
            SpawnItem();
            MarkSold();
            m_shopRoom.HandleSteal(this);
        }
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

        m_interactText.text = "SOLD";
        m_inputReader.Interact -= Interact;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (m_isSold || other.gameObject.layer != GameConstants.Layers.PlayerLayer) return;

        UpdateInteractText();
        m_interactText.enabled = true;
        m_inputReader.Interact += Interact;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer != GameConstants.Layers.PlayerLayer) return;

        m_interactText.enabled = false;
        m_inputReader.Interact -= Interact;
    }

    private void UpdateInteractText()
    {
        bool canAfford = m_creditManager != null && m_creditManager.Credits >= m_price;
        m_interactText.text = canAfford
            ? $"[E] Buy - {m_price} CR"
            : $"[E] Steal";
    }

    private void OnDestroy()
    {
        m_inputReader.Interact -= Interact;
    }
}
