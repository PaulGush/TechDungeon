using UnityEngine;

public interface IPickupEffect
{
    bool Apply(GameObject collector);

    /// <summary>
    /// Whether the effect would actually do something for this collector right now. Pickup
    /// checks this on trigger enter — if false, no prompt is shown and the pickup stays on the
    /// floor until the collector can use it (e.g. health pickup with the player at max HP).
    /// Defaults to true; effects with conditional applicability override.
    /// </summary>
    bool CanApply(GameObject collector) => true;
}
