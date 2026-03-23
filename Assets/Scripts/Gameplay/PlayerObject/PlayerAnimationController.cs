using System.Collections;
using Input;
using UnityEngine;

namespace PlayerObject
{
    public class PlayerAnimationController : MonoBehaviour
    {
        private static readonly int Horizontal = Animator.StringToHash("Horizontal");
        private static readonly int Vertical = Animator.StringToHash("Vertical");
        private static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");
        private static readonly int Death = Animator.StringToHash("Death");
        private static readonly int Roll = Animator.StringToHash("Roll");
        private static readonly WaitForSeconds FlashWait = new WaitForSeconds(0.2f);
        [Header("References")]
        [SerializeField] private Animator m_animator;
        [SerializeField] private SpriteRenderer m_spriteRenderer;
        [SerializeField] private InputReader m_inputReader;
        [SerializeField] private EntityHealth m_health;
        
        [Header("Settings")]
        [SerializeField] private Color m_healColor = Color.green;
        [SerializeField] private Color m_damageColor = Color.red;

        private void OnEnable()
        {
            m_inputReader.Move += OnMove;
            m_inputReader.Roll += OnRoll;

            m_health.OnTakeDamage += OnHurt;
            m_health.OnHeal += OnHeal;
            m_health.OnDeath += OnDeath;
        }

        private void OnDisable()
        {
            m_inputReader.Move -= OnMove;
            m_inputReader.Roll -= OnRoll;
            
            m_health.OnTakeDamage -= OnHurt;
            m_health.OnHeal -= OnHeal;
            m_health.OnDeath -= OnDeath; 
        }

        private void OnMove(Vector2 direction)
        {
            if (!m_inputReader.IsMoveInputPressed)
                return;
                
            m_animator.SetFloat(Horizontal, direction.x);
            m_animator.SetFloat(Vertical, direction.y);
        }

        private void OnRoll()
        {
            if (!m_inputReader.IsMoveInputPressed)
                return;
            
            m_animator.SetTrigger(Roll);
        }

        private void OnHurt()
        {
            StopAllCoroutines();
            StartCoroutine(ChangeColorForSeconds(m_damageColor));
        }

        private void OnHeal()
        {
            StopAllCoroutines();
            StartCoroutine(ChangeColorForSeconds(m_healColor));
        }

        private void OnDeath()
        {
            m_inputReader.DisablePlayerActions();
            m_animator.SetTrigger(Death);
        }

        public void ResetAnimator()
        {
            StopAllCoroutines();
            m_spriteRenderer.color = Color.white;
            m_animator.ResetTrigger(Death);
            m_animator.Rebind();
            m_animator.Update(0f);
        }

        private IEnumerator ChangeColorForSeconds(Color color)
        {
            m_spriteRenderer.color = color;
            yield return FlashWait;
            m_spriteRenderer.color = Color.white;
        }

        private void Update()
        {
            m_animator.SetFloat(MoveSpeed, m_inputReader.IsMoveInputPressed ? 1 : 0);
        }
    }
}