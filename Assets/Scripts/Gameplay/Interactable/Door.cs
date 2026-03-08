using UnityEngine;

public class Door : MonoBehaviour
{
    private static readonly int IsOpen = Animator.StringToHash("Open");
    [SerializeField] private Animator m_animator;

    protected virtual void Open()
    {
        m_animator.SetBool(IsOpen, true);
    }

    protected virtual void Close()
    {
        m_animator.SetBool(IsOpen, false);
    }

    protected virtual bool CanUnlock()
    {
        return true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer != GameConstants.Layers.PlayerLayer) return;
        
        if (CanUnlock() && !m_animator.GetBool(IsOpen))
        {
            Open();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer != GameConstants.Layers.PlayerLayer) return;
        
        if (m_animator.GetBool(IsOpen))
        {
            Close();
        }
    }
}