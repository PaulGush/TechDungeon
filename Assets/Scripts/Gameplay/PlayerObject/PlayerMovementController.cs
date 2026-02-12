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

        [Header("Settings")]
        [SerializeField] private float m_speed = 10f;
        [SerializeField] private float m_rollForce = 50f;
        [SerializeField] private float m_rollDuration = 0.2f;

        private float m_rollTimer;

        private void Awake()
        {
            ServiceLocator.Global.Register(this);
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
            Vector2 newPosition = m_rigidbody2D.position + direction * (m_speed * Time.fixedDeltaTime);
            m_rigidbody2D.MovePosition(newPosition);
        }

        public void Roll()
        {
            if (m_rollTimer > 0) return;

            m_rollTimer = m_rollDuration;
            m_rigidbody2D.linearVelocity = Vector2.zero;
            m_rigidbody2D.AddForce(m_inputReader.MoveDirection * m_rollForce, ForceMode2D.Impulse);
        }
    }
}