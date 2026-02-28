using UnityEngine;

[CreateAssetMenu(menuName = "Settings/Enemy Settings")]
public class EnemySettings : ScriptableObject
{
    [Header("Movement")]
    public float Speed = 10f;
    public float AttackRange = 1f;

    [Header("Combat")]
    public float FireRate = 0.5f;
}
