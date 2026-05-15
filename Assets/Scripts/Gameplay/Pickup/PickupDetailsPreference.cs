using System;

/// <summary>
/// A sticky, game-wide "show me the details" toggle for pickups. While it's on, walking near an
/// item/ability shows its tooltip and walking near a weapon shows its stat-bar panel — automatically,
/// without re-pressing the button at every pickup. Flipped by the ViewWeaponStats input (Left Alt /
/// left-stick click). Defaults to off so the player opts in.
/// </summary>
public static class PickupDetailsPreference
{
    public static bool ShowDetails { get; private set; }

    /// <summary>Raised whenever <see cref="ShowDetails"/> changes.</summary>
    public static event Action Changed;

    public static void Toggle()
    {
        ShowDetails = !ShowDetails;
        Changed?.Invoke();
    }
}
