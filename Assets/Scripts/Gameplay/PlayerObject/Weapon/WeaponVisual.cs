using UnityEngine;

namespace PlayerObject
{
    public class WeaponVisual : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer m_spriteRenderer;
        [SerializeField] private WeaponAnchor m_weaponAnchor;
        
        private void FixedUpdate()
        {
            m_spriteRenderer.flipY = m_weaponAnchor.transform.rotation.eulerAngles.z < 180;    
        }
    }
}
