using UnityEngine;

public class EnemyWeaponAnchor : MonoBehaviour
{
    [SerializeField] private EnemyTargeting _targeting;

    private void Update()
    {
        if (_targeting == null || _targeting.CurrentTarget == null) return;
        
        Vector3 diff = _targeting.CurrentTarget.position - transform.position;
        diff.Normalize();  
        float rotZ = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
    }
}