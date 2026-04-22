using UnityEngine;

public enum MutationType
{
    FlatDamage,
    PercentDamage,
    PercentSpeed,
    Pierce,
    MaxHealth,
    Armor,
    AmmoEfficiency
}

[CreateAssetMenu(menuName = "Data/Combat/Mutation")]
public class Mutation : ScriptableObject
{
    public string DisplayName;
    [TextArea] public string Description;
    public Sprite Icon;
    public MutationType Type;
    public float Value;

    public string GetEffectString()
    {
        switch (Type)
        {
            case MutationType.FlatDamage: return $"+{Value:0.#} Damage";
            case MutationType.PercentDamage: return $"+{Value:0.#}% Damage";
            case MutationType.PercentSpeed: return $"+{Value:0.#}% Move Speed";
            case MutationType.Pierce: return $"+{Value:0.#} Pierce";
            case MutationType.MaxHealth: return $"+{Value:0.#} Max Health";
            case MutationType.Armor: return $"+{Value:0.#} Armor";
            case MutationType.AmmoEfficiency: return $"{Value:0.#}% Chance to Save Ammo";
            default: return string.Empty;
        }
    }
}
