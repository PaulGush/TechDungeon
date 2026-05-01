// Polymorphic effect contract for ActiveAbility. Concrete implementations live under
// Assets/Scripts/Gameplay/Abilities/Effects/ and are referenced from an ActiveAbility
// asset via [SerializeReference] so a single ScriptableObject can carry effect-specific
// data without spawning a parallel SO type per behavior.
public interface IAbilityEffect
{
    void Execute(in AbilityContext ctx);
}
