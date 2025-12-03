using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerObject
{
    public class WeaponAnchor : MonoBehaviour
    {
        //TODO: Add controller support
        
        [SerializeField] private Camera _camera;
        private void FixedUpdate()
        {
            Vector3 diff = _camera.ScreenToWorldPoint(InputSystem.GetDevice<Mouse>().position.ReadValue()) - transform.position;
            diff.Normalize();  
            float rotZ = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, rotZ - 90);
        }
    }
}