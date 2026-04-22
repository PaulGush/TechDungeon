using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityServiceLocator;

public class MutationHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform m_mutationListParent;
    [SerializeField] private GameObject m_mutationEntryPrefab;

    private MutationManager m_mutationManager;

    private void Start()
    {
        ServiceLocator.Global.TryGet(out m_mutationManager);
        if (m_mutationManager == null) return;

        m_mutationManager.OnMutationAdded += OnMutationAdded;

        // Display any mutations already collected
        foreach (Mutation mutation in m_mutationManager.Mutations)
        {
            AddMutationEntry(mutation);
        }
    }

    private void OnDestroy()
    {
        if (m_mutationManager != null)
            m_mutationManager.OnMutationAdded -= OnMutationAdded;
    }

    private void OnMutationAdded(Mutation mutation)
    {
        AddMutationEntry(mutation);
    }

    private void AddMutationEntry(Mutation mutation)
    {
        GameObject entry = Instantiate(m_mutationEntryPrefab, m_mutationListParent);
        entry.SetActive(true);

        // Set icon if available
        Image icon = entry.GetComponentInChildren<Image>();
        if (icon != null && mutation.Icon != null)
        {
            icon.sprite = mutation.Icon;
            icon.enabled = true;
        }

        // Set name text
        TextMeshProUGUI text = entry.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = mutation.DisplayName;
        }

        TooltipTrigger trigger = entry.GetComponent<TooltipTrigger>();
        if (trigger == null)
            trigger = entry.AddComponent<TooltipTrigger>();
        trigger.Setup(mutation.DisplayName, mutation.Description, mutation.GetEffectString());
    }

    public void ClearDisplay()
    {
        for (int i = m_mutationListParent.childCount - 1; i >= 0; i--)
        {
            Destroy(m_mutationListParent.GetChild(i).gameObject);
        }
    }
}
