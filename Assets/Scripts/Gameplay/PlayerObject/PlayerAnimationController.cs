using Input;
using UnityEngine;

namespace PlayerObject
{
    public class PlayerAnimationController : MonoBehaviour
    {
        private static readonly int Horizontal = Animator.StringToHash("Horizontal");
        private static readonly int Vertical = Animator.StringToHash("Vertical");
        private static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");
        private static readonly int Hurt = Animator.StringToHash("Hurt");
        private static readonly int Heal = Animator.StringToHash("Heal");
        private static readonly int Death = Animator.StringToHash("Death");
        private static readonly int Roll = Animator.StringToHash("Roll");
        [SerializeField] private Animator _animator;
        [SerializeField] private InputReader _inputReader;
        [SerializeField] private EntityHealth _health;

        private void OnEnable()
        {
            _inputReader.EnablePlayerActions();
            _inputReader.Move += OnMove;
            _inputReader.Roll += OnRoll;
            
            _health.OnTakeDamage += () => _animator.SetTrigger(Hurt);
            _health.OnHeal += () => _animator.SetTrigger(Heal);
            _health.OnDeath += () => _animator.SetTrigger(Death);
        }

        private void OnDisable()
        {
            _inputReader.Move -= OnMove;
            _inputReader.Roll -= OnRoll;
            
            _health.OnTakeDamage -= () => _animator.SetTrigger(Hurt);
            _health.OnHeal -= () => _animator.SetTrigger(Heal);
            _health.OnDeath -= () => _animator.SetTrigger(Death);
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

        private void Update()
        {
            _animator.SetFloat(MoveSpeed, _inputReader.IsMoveInputPressed ? 1 : 0);
        }
    }
}