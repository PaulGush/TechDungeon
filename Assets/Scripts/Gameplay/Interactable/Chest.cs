using Input;
using TMPro;
using UnityEngine;

public class Chest : MonoBehaviour, IInteractable
{
    private static readonly int Open = Animator.StringToHash("Open");
    [SerializeField] private Animator m_animator;
    [SerializeField] private TextMeshPro m_interactText;
    [SerializeField] private InputReader m_inputReader;
    
    private bool m_isOpen;

    public void Interact()
    {
        if (m_isOpen) return;
        
        m_animator.SetTrigger(Open);
        m_interactText.enabled = false;
        m_isOpen = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (m_isOpen || other.gameObject.layer != LayerMask.NameToLayer("Player")) return;
        m_interactText.enabled = true;
        m_inputReader.Interact += Interact;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (m_isOpen|| other.gameObject.layer != LayerMask.NameToLayer("Player")) return;
        m_interactText.enabled = false;
        m_inputReader.Interact -= Interact;
    }
}