using UnityEngine;

/// <summary>
/// Heavy sealed door used for transitioning between rooms.
/// Displays a reward icon indicating what awaits in the next room.
/// The player must walk all the way through to trigger the room transition.
/// Starts locked in combat/boss rooms and unlocks when the room is cleared.
/// Requires a child GameObject with a BulkheadTransitionTrigger placed past the door threshold.
/// </summary>
public class BulkheadDoor : Door
{
    [Header("Bulkhead")]
    [SerializeField] private SpriteRenderer m_rewardIcon;
    [SerializeField] private BulkheadTransitionTrigger m_transitionTrigger;

    private RoomSettings m_roomSettings;
    private RoomManager m_roomManager;
    private bool m_isUsed;
    private bool m_isLocked;

    public bool IsLocked => m_isLocked;

    public void Initialize(RoomSettings settings, RoomManager roomManager)
    {
        m_roomSettings = settings;
        m_roomManager = roomManager;

        if (m_rewardIcon != null && settings.RoomIcon != null)
        {
            m_rewardIcon.sprite = settings.RoomIcon;
        }
    }

    public void Lock()
    {
        m_isLocked = true;
    }

    public void Unlock()
    {
        m_isLocked = false;
    }

    protected override bool CanUnlock()
    {
        return !m_isLocked && !m_isUsed && m_roomSettings != null;
    }

    private void OnEnable()
    {
        if (m_transitionTrigger != null)
        {
            m_transitionTrigger.OnPlayerPassedThrough += HandlePlayerPassedThrough;
        }
    }

    private void OnDisable()
    {
        if (m_transitionTrigger != null)
        {
            m_transitionTrigger.OnPlayerPassedThrough -= HandlePlayerPassedThrough;
        }
    }

    private void HandlePlayerPassedThrough()
    {
        if (m_isUsed || m_isLocked) return;

        m_isUsed = true;
        m_roomManager.LoadRoom(m_roomSettings);
    }
}
