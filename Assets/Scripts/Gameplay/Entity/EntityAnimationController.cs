using UnityEngine;

public class EntityAnimationController : MonoBehaviour
{
    private static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");
    
    [SerializeField] protected Animator _animator;
}