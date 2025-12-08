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
        [Header("References")]
        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private InputReader _inputReader;
        [SerializeField] private EntityHealth _health;
        
        [Header("Settings")]
        [SerializeField] private Color _healColor = Color.green;
        [SerializeField] private Color _damageColor = Color.red;

        private void OnEnable()
        {
            _inputReader.EnablePlayerActions();
            _inputReader.Move += OnMove;
            _inputReader.Roll += OnRoll;

            _health.OnTakeDamage += OnHurt;
            _health.OnHeal += OnHeal;
            _health.OnDeath += OnDeath;
        }

        private void OnDisable()
        {
            _inputReader.Move -= OnMove;
            _inputReader.Roll -= OnRoll;
            
            _health.OnTakeDamage -= OnHurt;
            _health.OnHeal -= OnHeal;
            _health.OnDeath -= OnDeath; 
        }

        private void OnMove(Vector2 direction)
        {
            if (!_inputReader.IsMoveInputPressed)
                return;
                
            _animator.SetFloat(Horizontal, direction.x);
            _animator.SetFloat(Vertical, direction.y);
        }

        private void OnRoll()
        {
            _animator.SetTrigger(Roll);
        }

        private void OnHurt()
        {
            StopAllCoroutines();
            StartCoroutine(ChangeColorForSeconds(_damageColor, 0.2f));
        }
        
        private void OnHeal()
        {
            StopAllCoroutines();
            StartCoroutine(ChangeColorForSeconds(_healColor, 0.2f));
        }

        private void OnDeath()
        {
            _inputReader.DisablePlayerActions();
            _animator.SetTrigger(Death);
        }

        private IEnumerator ChangeColorForSeconds(Color color, float seconds)
        {
            _spriteRenderer.color = color;
            yield return new WaitForSeconds(seconds);
            _spriteRenderer.color = Color.white;
        }

        private void Update()
        {
            _animator.SetFloat(MoveSpeed, _inputReader.IsMoveInputPressed ? 1 : 0);
        }
    }
}