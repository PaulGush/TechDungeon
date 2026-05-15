public interface IAmmoEffect
{
    void OnHit(AmmoEffectContext ctx);
    void OnDestroy(AmmoEffectContext ctx);
    bool TryPreventDestroy(AmmoEffectContext ctx);

    // Per-fixed-step hook for in-flight steering or other continuous effects.
    void OnTick(AmmoEffectContext ctx);
}
