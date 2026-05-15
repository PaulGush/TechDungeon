using UnityEngine;

[CreateAssetMenu(menuName = "Data/Entity/Enemy Settings")]
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

    [Header("Strafe")]
    [Tooltip("How strongly strafing enemies correct back toward their PreferredAttackDistance. Higher values close/open gaps faster.")]
    public float StrafeDistanceCorrectionStrength = 0.5f;
    [Tooltip("Maximum absolute distance delta (in units) that contributes to the strafe distance correction.")]
    public float StrafeDistanceCorrectionClamp = 1f;

    [Header("Alert")]
    [Tooltip("Time in seconds before the enemy deactivates if it cannot reach its target. 0 = never deactivate.")]
    public float AlertDuration = 0f;

    [Header("Combat")]
    public float FireRate = 0.5f;
    public float PreferredAttackDistance = 0f;
    public float StrafeSpeed = 3f;
}
