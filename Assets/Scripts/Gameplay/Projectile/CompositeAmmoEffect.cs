/// <summary>
/// Runs several <see cref="IAmmoEffect"/>s on one projectile so a weapon's intrinsic round
/// behaviour (e.g. the RPG missile's explode-on-impact) keeps working alongside whatever
/// special ammo the player has loaded — ricochet, seeking, and so on. Every call is forwarded
/// to all members; the one special case is <see cref="TryPreventDestroy"/>, which stops at the
/// first member that claims the hit so a second prevent-destroy effect can't also run its side
/// effects (e.g. so an explosive+ricochet missile bounces off a wall instead of detonating, but
/// still detonates when it hits an enemy or runs out of bounces).
/// </summary>
public class CompositeAmmoEffect : IAmmoEffect
{
    private readonly IAmmoEffect[] m_effects;

    public CompositeAmmoEffect(params IAmmoEffect[] effects) => m_effects = effects;

    /// <summary>
    /// Returns <paramref name="a"/>, <paramref name="b"/>, a composite of both (a runs first),
    /// or null — whichever is the simplest representation. Lets callers compose freely without
    /// allocating a wrapper for the common one-or-zero-effect cases.
    /// </summary>
    public static IAmmoEffect Compose(IAmmoEffect a, IAmmoEffect b)
    {
        if (a == null) return b;
        if (b == null) return a;
        return new CompositeAmmoEffect(a, b);
    }

    public void OnHit(AmmoEffectContext ctx)
    {
        foreach (IAmmoEffect e in m_effects) e?.OnHit(ctx);
    }

    public void OnTick(AmmoEffectContext ctx)
    {
        foreach (IAmmoEffect e in m_effects) e?.OnTick(ctx);
    }

    public void OnDestroy(AmmoEffectContext ctx)
    {
        foreach (IAmmoEffect e in m_effects) e?.OnDestroy(ctx);
    }

    public bool TryPreventDestroy(AmmoEffectContext ctx)
    {
        foreach (IAmmoEffect e in m_effects)
            if (e != null && e.TryPreventDestroy(ctx)) return true;
        return false;
    }
}
