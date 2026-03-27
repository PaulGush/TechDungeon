using Input;
using PlayerObject;
using UnityEngine;
using UnityServiceLocator;

public class Chest : MonoBehaviour, IInteractable
{
    private const float GizmoRadius = 0.1f;
    private static readonly int Open = Animator.StringToHash("Open");

    [Header("References")]
    [SerializeField] private Animator m_animator;
    [SerializeField] private InputReader m_inputReader;
    [SerializeField] private ChestSettings m_settings;

    [Header("Settings")]
    [SerializeField] private Transform m_itemSpawnPoint;
    [SerializeField] private float m_verticalSpawnDistance = 1;
    [SerializeField] private float m_horizontalSpawnOffset = 1;
    [SerializeField] private Vector3 m_aboveChestTargetPosition;

    private RoomManager m_roomManager;
    private PlayerInteractionDisplay m_interactionDisplay;
    private bool m_isOpen;
    private bool m_isLocked;

    public void Lock() => m_isLocked = true;
    public void Unlock() => m_isLocked = false;

    public void SetSettings(ChestSettings settings)
    {
        m_settings = settings;
    }

    public void SetSettings(ChestSettings settings)
    {
        m_settings = settings;
    }

    private RoomManager RoomManager => m_roomManager ??= ServiceLocator.Global.Get<RoomManager>();

    private void OnDestroy()
    {
        m_inputReader.Interact -= Interact;
    }

    public void Interact()
    {
        if (m_isOpen || m_isLocked) return;

        m_animator.SetTrigger(Open);
        m_interactionDisplay?.Hide(this);
        m_isOpen = true;
        m_inputReader.Interact -= Interact;

        Transform roomParent = RoomManager.CurrentRoomTransform;

        float itemIndex = 0;
        foreach (Lootable lootableItem in m_settings.GetRandomItems())
        {
            if (lootableItem == null) continue;
            Vector3 targetPosition = new Vector3((m_itemSpawnPoint.position.x + itemIndex) - m_horizontalSpawnOffset, m_itemSpawnPoint.position.y - m_verticalSpawnDistance, m_itemSpawnPoint.position.z);
            GameObject item = Instantiate(lootableItem.gameObject, m_itemSpawnPoint.position, Quaternion.identity, roomParent);
            SpriteRenderer sr = item.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) item.name = sr.sprite.name;

            Lootable lootableComp = item.GetComponent<Lootable>();

            lootableComp.ChangeRarity(LootableRarity.DetermineRarity(m_settings.LegendaryDropChance, m_settings.EpicDropChance, m_settings.RareDropChance, m_settings.UncommonDropChance));
            lootableComp.SetTargetPosition(targetPosition);
            lootableComp.SetAboveChestTargetPosition(transform.position + m_aboveChestTargetPosition);
            lootableComp.StartSpawnSequence(m_settings.TotalSpawnTime, m_settings.SpawnTimeInterval, 0);

            itemIndex++;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (m_isOpen || m_isLocked || other.gameObject.layer != GameConstants.Layers.PlayerLayer) return;
        m_interactionDisplay ??= ServiceLocator.Global.Get<PlayerInteractionDisplay>();
        m_interactionDisplay.Show("[E]", this);
        m_inputReader.Interact += Interact;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (m_isOpen || other.gameObject.layer != GameConstants.Layers.PlayerLayer) return;
        m_interactionDisplay?.Hide(this);
        m_inputReader.Interact -= Interact;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        for(int i = 0; i < m_settings.ItemDropCount; i++)
        {
            Gizmos.DrawWireSphere(new Vector3(transform.position.x + i - m_horizontalSpawnOffset, transform.position.y - m_verticalSpawnDistance, transform.position.z), GizmoRadius);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + m_aboveChestTargetPosition, GizmoRadius);
    }
}
