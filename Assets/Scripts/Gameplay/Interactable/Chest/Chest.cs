using System;
using Input;
using TMPro;
using UnityEngine;

public class Chest : MonoBehaviour, IInteractable
{
    private static readonly int Open = Animator.StringToHash("Open");
    
    [Header("References")]
    [SerializeField] private Animator m_animator;
    [SerializeField] private TextMeshPro m_interactText;
    [SerializeField] private InputReader m_inputReader;
    [SerializeField] private ChestSettings m_settings;

    [Header("Settings")] 
    [SerializeField] private Transform m_itemSpawnPoint;
    [SerializeField] private float m_verticalSpawnDistance = 1;
    [SerializeField] private float m_horizontalSpawnOffset = 1;
    
    private bool m_isOpen;

    public void Interact()
    {
        if (m_isOpen) return;
        
        m_animator.SetTrigger(Open);
        m_interactText.enabled = false;
        m_isOpen = true;

        float i = 0;
        foreach (Lootable lootableItem in m_settings.GetRandomItems())
        {
            Vector3 aboveChestTargetPosition = new Vector3(transform.position.x, transform.position.y + 1, transform.position.z);
            Vector3 targetPosition = new Vector3((m_itemSpawnPoint.position.x + i) - m_horizontalSpawnOffset, m_itemSpawnPoint.position.y - m_verticalSpawnDistance, m_itemSpawnPoint.position.z);
            GameObject item = Instantiate(lootableItem.gameObject, m_itemSpawnPoint.position, Quaternion.identity);
            item.name = item.GetComponent<SpriteRenderer>().sprite.name;

            Lootable lootableComp = item.GetComponent<Lootable>();
            
            lootableComp.ChangeRarity(LootableRarity.DetermineRarity(lootableItem, m_settings.EpicDropChance, m_settings.RareDropChance, m_settings.UncommonDropChance));
            lootableComp.SetTargetPosition(targetPosition);
            lootableComp.SetAboveChestTargetPosition(aboveChestTargetPosition);
            lootableComp.StartSpawnSequence(m_settings.TotalSpawnTime, m_settings.SpawnTimeInterval, 0);
            
            i++;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (m_isOpen || other.gameObject.layer != LayerMask.NameToLayer("Player")) return;
        m_interactText.enabled = true;
        m_inputReader.Interact += Interact;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (m_isOpen|| other.gameObject.layer != LayerMask.NameToLayer("Player")) return;
        m_interactText.enabled = false;
        m_inputReader.Interact -= Interact;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        
        for(int i = 0; i < m_settings.ItemDropCount; i++)
        {
            Gizmos.DrawWireSphere(new Vector3(transform.position.x + i - m_horizontalSpawnOffset, transform.position.y - m_verticalSpawnDistance, transform.position.z), 0.1f);
        }
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(new Vector3(transform.position.x, transform.position.y + 1, transform.position.z), 0.1f);
    }
}