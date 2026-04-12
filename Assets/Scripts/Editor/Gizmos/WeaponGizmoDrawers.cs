using UnityEditor;
using UnityEngine;

namespace TechDungeon.Editor
{
    public static class WeaponGizmoDrawers
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        private static void DrawWeaponShooting(WeaponShooting shooting, GizmoType gizmoType)
        {
            var so = new SerializedObject(shooting);
            var shootPointProp = so.FindProperty("m_shootPoint");
            if (shootPointProp == null || shootPointProp.objectReferenceValue == null)
                return;

            var shootPoint = (Transform)shootPointProp.objectReferenceValue;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(shootPoint.position, 0.08f);

            Vector3 dir = shootPoint.right;
            Vector3 tip = shootPoint.position + dir * 0.4f;
            Gizmos.DrawLine(shootPoint.position, tip);
            Gizmos.DrawLine(tip, tip - dir * 0.1f + (Vector3)(Vector2.Perpendicular(dir) * 0.06f));
            Gizmos.DrawLine(tip, tip - dir * 0.1f - (Vector3)(Vector2.Perpendicular(dir) * 0.06f));
        }
    }
}
