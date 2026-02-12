using UnityEngine;

public class EnemyWeaponAnchor : MonoBehaviour
{
    [SerializeField] private EnemyTargeting m_targeting;

    private void Update()
    {
        if (m_targeting == null || m_targeting.CurrentTarget == null) return;
        
        Vector3 diff = m_targeting.CurrentTarget.position - transform.position;
        diff.Normalize();  
        float rotZ = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
    }
}