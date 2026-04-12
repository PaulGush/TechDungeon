using System;
using UnityEngine;

/// <summary>
/// Placed on a child GameObject past the door threshold.
/// Fires when the player walks through the bulkhead door.
/// </summary>
public class BulkheadTransitionTrigger : MonoBehaviour
{
    public event Action OnPlayerPassedThrough;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer != GameConstants.Layers.PlayerLayer) return;
        OnPlayerPassedThrough?.Invoke();
    }
}
