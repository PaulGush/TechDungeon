using Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerObject
{
    public class WeaponAnchor : MonoBehaviour
    {
        [SerializeField] private Camera m_camera;
        [SerializeField] private InputReader m_inputReader;

        private Vector2 m_previousFrameMousePosition;
        
        private void FixedUpdate()
        {
            // Use controller look if available, otherwise fall back to mouse
            Vector2 lookDirection = m_inputReader.LookDirection;
            if (lookDirection.sqrMagnitude > 0.1f)
            {
                // Controller input
                float rotZ = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, rotZ - 90);
            }
            else if (m_previousFrameMousePosition != Mouse.current.position.ReadValue())
            {
                // Mouse input
                Vector3 diff = m_camera.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - transform.position;
                diff.Normalize();  
                float rotZ = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, rotZ - 90);
            }
            m_previousFrameMousePosition = Mouse.current.position.ReadValue();
        }
    }
}