public interface IAmmoEffect
{
    void OnHit(AmmoEffectContext ctx);
    void OnDestroy(AmmoEffectContext ctx);
    bool TryPreventDestroy(AmmoEffectContext ctx);
}
