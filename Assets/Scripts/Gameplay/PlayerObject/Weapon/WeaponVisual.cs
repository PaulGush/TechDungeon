using UnityEngine;

namespace PlayerObject
{
    public class WeaponVisual : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private WeaponAnchor _weaponAnchor;
        
        private void FixedUpdate()
        {
            _spriteRenderer.flipY = _weaponAnchor.transform.rotation.eulerAngles.z < 180;    
        }
    }
}
