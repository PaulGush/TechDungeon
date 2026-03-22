using Input;
using UnityEngine;
using UnityServiceLocator;

namespace PlayerObject
{
    public class PlayerMovementController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputReader m_inputReader;
        [SerializeField] private Rigidbody2D m_rigidbody2D;
        [SerializeField] private PlayerSettings m_settings;

        private float m_rollTimer;
        private MutationManager m_mutationManager;

        private void Awake()
        {
            ServiceLocator.Global.Register(this);
        }

        private void Start()
        {
            ServiceLocator.Global.TryGet(out m_mutationManager);
        }

        private void OnEnable()
        {
            m_inputReader.EnablePlayerActions();
        }

        private void OnDisable()
        {
            m_inputReader.DisablePlayerActions();
        }

        private void FixedUpdate()
        {
            if (m_rollTimer > 0)
            {
                m_rollTimer -= Time.fixedDeltaTime;
                return;
            }

            Vector2 direction = m_inputReader.MoveDirection;
            float speed = m_settings.Speed;
            if (m_mutationManager != null)
                speed *= m_mutationManager.GetSpeedMultiplier();
            Vector2 newPosition = m_rigidbody2D.position + direction * (speed * Time.fixedDeltaTime);
            m_rigidbody2D.MovePosition(newPosition);
        }

        public void Roll()
        {
            if (m_rollTimer > 0) return;

            m_rollTimer = m_settings.RollDuration;
            m_rigidbody2D.linearVelocity = Vector2.zero;
            m_rigidbody2D.AddForce(m_inputReader.MoveDirection * m_settings.RollForce, ForceMode2D.Impulse);
        }
    }
}
