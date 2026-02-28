using UnityEngine;

public class EnemyWeaponAnchor : MonoBehaviour
{
    [SerializeField] private EnemyTargeting m_targeting;

    private void Update()
    {
        if (m_targeting == null || m_targeting.CurrentTarget == null) return;

        Vector3 diff = (m_targeting.CurrentTarget.position - transform.position).normalized;
        transform.rotation = MathUtilities.CalculateAimRotation(diff);
    }
}
