using Input;
using UnityEngine;

namespace PlayerObject
{
    public class PlayerAnimationController : MonoBehaviour
    {
        private static readonly int Horizontal = Animator.StringToHash("Horizontal");
        private static readonly int Vertical = Animator.StringToHash("Vertical");
        private static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");
        [SerializeField] private Animator _animator;
        [SerializeField] private InputReader _inputReader;

        private void OnEnable()
        {
            _inputReader.EnablePlayerActions();
            _inputReader.Move += OnMove;
        }

        private void OnDisable()
        {
            _inputReader.Move -= OnMove;
        }

        private void OnMove(Vector2 direction)
        {
            if (!_inputReader.IsMoveInputPressed)
                return;
                
            _animator.SetFloat(Horizontal, direction.x);
            _animator.SetFloat(Vertical, direction.y);
        }

        private void Update()
        {
            _animator.SetFloat(MoveSpeed, _inputReader.IsMoveInputPressed ? 1 : 0);
        }
    }
}