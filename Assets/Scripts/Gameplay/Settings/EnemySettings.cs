using UnityEngine;

[CreateAssetMenu(menuName = "Settings/Enemy Settings")]
public class EnemySettings : ScriptableObject
{
    [Header("Movement")]
    public float Speed = 10f;
    public float AttackRange = 1f;

    [Header("Obstacle Avoidance")]
    public float ObstacleAvoidanceDistance = 2f;
    public LayerMask ObstacleLayerMask;
    [Range(8, 16)] public int SteeringRayCount = 8;
    public float StuckThreshold = 0.1f;
    public float StuckTimeBeforeEscape = 0.5f;

    [Header("Separation")]
    public float SeparationRadius = 1.5f;
    public float SeparationWeight = 1.5f;

    [Header("Combat")]
    public float FireRate = 0.5f;
    public float PreferredAttackDistance = 0f;
    public float StrafeSpeed = 3f;
}
