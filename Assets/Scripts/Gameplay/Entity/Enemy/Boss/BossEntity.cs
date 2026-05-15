using UnityEngine;

/// <summary>
/// Marker component placed on boss enemy prefabs. Systems that need to single out
/// "the boss" of a room (camera tracking, music cues, room-clear bookkeeping, etc.)
/// detect bosses by the presence of this component rather than by prefab identity,
/// so the rule stays generic across any future boss prefab.
/// </summary>
public class BossEntity : MonoBehaviour
{
}
