using UnityEngine;

[CreateAssetMenu(menuName = "Data/Combat/Projectile Settings")]
public class ProjectileSettings : ScriptableObject
{
    public int Damage;
    public float Speed;
    public float Lifetime;
    public int HitsBeforeDeath;
}
