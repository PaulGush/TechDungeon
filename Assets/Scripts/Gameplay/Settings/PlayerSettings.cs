using UnityEngine;

[CreateAssetMenu(menuName = "Data/Entity/Player Settings")]
public class PlayerSettings : ScriptableObject
{
    [Header("Movement")]
    public float Speed = 10f;
    public float RollForce = 50f;
    public float RollDuration = 0.2f;
}
