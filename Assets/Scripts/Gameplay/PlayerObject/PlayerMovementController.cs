using System;
using Input;
using UnityEngine;
using UnityServiceLocator;

namespace PlayerObject
{
    public class PlayerMovementController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputReader _inputReader;
        [SerializeField] private Rigidbody2D _rigidbody2D;

        [Header("Settings")]
        [SerializeField] private float _speed = 10f;
        [SerializeField] private float _rollForce = 50f;
        [SerializeField] private float _rollDuration = 0.2f;

        private float _rollTimer;

        private void Awake()
        {
            ServiceLocator.Global.Register(this);
        }

        private void OnEnable()
        {
            _inputReader.EnablePlayerActions();
        }

        private void OnDisable()
        {
            _inputReader.DisablePlayerActions();
        }

        private void FixedUpdate()
        {
            if (_rollTimer > 0)
            {
                _rollTimer -= Time.fixedDeltaTime;
                return;
            }
            
            Vector2 direction = _inputReader.MoveDirection;
            Vector2 newPosition = _rigidbody2D.position + direction * (_speed * Time.fixedDeltaTime);
            _rigidbody2D.MovePosition(newPosition);
        }

        public void Roll()
        {
            if (_rollTimer > 0) return;

            _rollTimer = _rollDuration;
            _rigidbody2D.linearVelocity = Vector2.zero;
            _rigidbody2D.AddForce(_inputReader.MoveDirection * _rollForce, ForceMode2D.Impulse);
        }
    }
}