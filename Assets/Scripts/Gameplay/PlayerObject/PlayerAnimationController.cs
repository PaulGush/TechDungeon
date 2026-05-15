using Input;
using UnityEngine;
using UnityServiceLocator;

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
        [SerializeField] private Animator m_animator;
        [SerializeField] private InputReader m_inputReader;
        [SerializeField] private EntityHealth m_health;

        [Tooltip("Impulse amplitude applied to the camera shake service when the player takes damage. Zero disables.")]
        [SerializeField] private float m_damageShakeAmplitude = 0.4f;

        private CameraShake m_cameraShake;

        private void OnEnable()
        {
            m_inputReader.Move += OnMove;
            m_inputReader.Roll += OnRoll;

            m_health.OnTakeDamage += OnHurt;
            m_health.OnDeath += OnDeath;
        }

        private void OnDisable()
        {
            m_inputReader.Move -= OnMove;
            m_inputReader.Roll -= OnRoll;

            m_health.OnTakeDamage -= OnHurt;
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
            if (m_cameraShake == null)
                ServiceLocator.Global.TryGet(out m_cameraShake);
            if (m_cameraShake != null && m_damageShakeAmplitude > 0f)
                m_cameraShake.Shake(m_damageShakeAmplitude);
        }

        private void OnDeath()
        {
            m_inputReader.DisablePlayerActions();
            m_animator.SetTrigger(Death);
        }

        public void ResetAnimator()
        {
            m_animator.ResetTrigger(Death);
            m_animator.Rebind();
            m_animator.Update(0f);
        }

        private void Update()
        {
            m_animator.SetFloat(MoveSpeed, m_inputReader.IsMoveInputPressed ? 1 : 0);
        }
    }
}
