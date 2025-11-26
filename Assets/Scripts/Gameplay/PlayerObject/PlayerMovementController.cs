using Input;
using UnityEngine;

namespace PlayerObject
{
    public class PlayerMovementController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputReader _inputReader;

        [Header("Settings")]
        [SerializeField] private float _speed = 10f;
    
        private void OnEnable()
        {
            _inputReader.EnablePlayerActions();
        }

        private void Update()
        {
            transform.Translate(_inputReader.Direction * (_speed * Time.deltaTime));
        }
    }
}