using System;
using Input;
using UnityEngine;

namespace PlayerObject
{
    public class PlayerMovementController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputReader _inputReader;
        [SerializeField] private Rigidbody2D _rigidbody2D;

        [Header("Settings")]
        [SerializeField] private float _speed = 10f;
    
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
            Vector2 direction = _inputReader.Direction;
            Vector2 newPosition = _rigidbody2D.position + direction * (_speed * Time.fixedDeltaTime);
            _rigidbody2D.MovePosition(newPosition);
        }
    }
}