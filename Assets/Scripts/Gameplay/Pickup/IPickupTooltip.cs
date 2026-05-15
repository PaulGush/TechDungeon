/// <summary>
/// Optional companion to <see cref="IPickupEffect"/>: a pickup that carries enough information to
/// describe itself (items, abilities) implements this so <see cref="Pickup"/> can pop a tooltip
/// alongside the interact prompt. Simple drops (health, credits, ammo) don't implement it.
/// </summary>
public interface IPickupTooltip
{
    bool TryGetTooltip(out string title, out string body, out string effect);
}
