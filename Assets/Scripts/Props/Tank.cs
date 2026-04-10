using UnityEngine;

public class Tank : MonoBehaviour
{
    private static readonly int Empty = Animator.StringToHash("Empty");
    [SerializeField] private Animator m_animator;
    [SerializeField] private bool m_empty;
    
    private void Start()
    {
        m_animator?.SetBool(Empty, m_empty);
    }
}
