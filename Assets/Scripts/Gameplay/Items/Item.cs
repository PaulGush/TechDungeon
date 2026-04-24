using UnityEngine;

public enum ItemType
{
    FlatDamage,
    PercentDamage,
    PercentSpeed,
    Pierce,
    MaxHealth,
    Armor,
    AmmoEfficiency
}

[CreateAssetMenu(menuName = "Data/Combat/Item")]
public class Item : ScriptableObject
{
    public string DisplayName;
    [TextArea] public string Description;
    public Sprite Icon;
    public ItemType Type;
    public float Value;

    public string GetEffectString()
    {
        switch (Type)
        {
            case ItemType.FlatDamage: return $"+{Value:0.#} Damage";
            case ItemType.PercentDamage: return $"+{Value:0.#}% Damage";
            case ItemType.PercentSpeed: return $"+{Value:0.#}% Move Speed";
            case ItemType.Pierce: return $"+{Value:0.#} Pierce";
            case ItemType.MaxHealth: return $"+{Value:0.#} Max Health";
            case ItemType.Armor: return $"+{Value:0.#} Armor";
            case ItemType.AmmoEfficiency: return $"{Value:0.#}% Chance to Save Ammo";
            default: return string.Empty;
        }
    }
}
