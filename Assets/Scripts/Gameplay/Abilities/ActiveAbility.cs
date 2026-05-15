using UnityEngine;

[CreateAssetMenu(menuName = "Data/Combat/Active Ability")]
public class ActiveAbility : ScriptableObject
{
    public string DisplayName;
    [TextArea] public string Description;
    public Sprite Icon;
    public Color TintColor = Color.white;

    [Min(0.05f)] public float Cooldown = 8f;

    [Tooltip("Polymorphic behavior. Pick a concrete IAbilityEffect implementation; per-effect data is authored on the chosen reference.")]
    [SerializeReference] public IAbilityEffect Effect;
}
