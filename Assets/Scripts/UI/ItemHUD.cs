using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityServiceLocator;

public class ItemHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform m_itemListParent;
    [SerializeField] private GameObject m_itemEntryPrefab;

    private ItemManager m_itemManager;

    private void Start()
    {
        ServiceLocator.Global.TryGet(out m_itemManager);
        if (m_itemManager == null) return;

        m_itemManager.OnItemAdded += OnItemAdded;

        // Display any items already collected
        foreach (Item item in m_itemManager.Items)
        {
            AddItemEntry(item);
        }
    }

    private void OnDestroy()
    {
        if (m_itemManager != null)
            m_itemManager.OnItemAdded -= OnItemAdded;
    }

    private void OnItemAdded(Item item)
    {
        AddItemEntry(item);
    }

    private void AddItemEntry(Item item)
    {
        GameObject entry = Instantiate(m_itemEntryPrefab, m_itemListParent);
        entry.SetActive(true);

        // Set icon if available
        Image icon = entry.GetComponentInChildren<Image>();
        if (icon != null && item.Icon != null)
        {
            icon.sprite = item.Icon;
            icon.enabled = true;
        }

        // Set name text
        TextMeshProUGUI text = entry.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = item.DisplayName;
        }

        TooltipTrigger trigger = entry.GetComponent<TooltipTrigger>();
        if (trigger == null)
            trigger = entry.AddComponent<TooltipTrigger>();
        trigger.Setup(item.DisplayName, item.Description, item.GetEffectString());
    }

    public void ClearDisplay()
    {
        for (int i = m_itemListParent.childCount - 1; i >= 0; i--)
        {
            Destroy(m_itemListParent.GetChild(i).gameObject);
        }
    }
}
