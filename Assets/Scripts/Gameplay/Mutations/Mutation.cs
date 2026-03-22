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

[CreateAssetMenu(menuName = "Mutations/Mutation")]
public class Mutation : ScriptableObject
{
    public string DisplayName;
    [TextArea] public string Description;
    public Sprite Icon;
    public MutationType Type;
    public float Value;
}
