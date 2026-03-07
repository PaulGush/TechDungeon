using UnityEditor;
using UnityEngine;

namespace TechDungeon.Editor
{
    public static class EnemyGizmoDrawers
    {
        [DrawGizmo(GizmoType.NonSelected)]
        private static void DrawEnemyMovementWire(EnemyMovement movement, GizmoType gizmoType)
        {
            DrawAttackRange(movement, false);
        }

        [DrawGizmo(GizmoType.Selected)]
        private static void DrawEnemyMovementSelected(EnemyMovement movement, GizmoType gizmoType)
        {
            DrawAttackRange(movement, true);
        }

        private static void DrawAttackRange(EnemyMovement movement, bool filled)
        {
            var so = new SerializedObject(movement);
            var settingsProp = so.FindProperty("m_settings");
            if (settingsProp == null || settingsProp.objectReferenceValue == null)
                return;

            var settingsSo = new SerializedObject(settingsProp.objectReferenceValue);
            var attackRangeProp = settingsSo.FindProperty("AttackRange");
            if (attackRangeProp == null)
                return;

            float range = attackRangeProp.floatValue;
            Vector3 pos = movement.transform.position;

            if (filled)
            {
                Handles.color = new Color(1f, 0f, 0f, 0.1f);
                Handles.DrawSolidDisc(pos, Vector3.forward, range);
            }

            Handles.color = new Color(1f, 0f, 0f, 0.5f);
            Handles.DrawWireDisc(pos, Vector3.forward, range);
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        private static void DrawEnemyShooting(EnemyShooting shooting, GizmoType gizmoType)
        {
            var so = new SerializedObject(shooting);
            var shootPointProp = so.FindProperty("m_shootPoint");
            if (shootPointProp == null || shootPointProp.objectReferenceValue == null)
                return;

            var shootPoint = (Transform)shootPointProp.objectReferenceValue;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(shootPoint.position, 0.08f);

            // Arrow in shoot direction
            Vector3 dir = shootPoint.right;
            Vector3 tip = shootPoint.position + dir * 0.4f;
            Gizmos.DrawLine(shootPoint.position, tip);
            Gizmos.DrawLine(tip, tip - dir * 0.1f + (Vector3)(Vector2.Perpendicular(dir) * 0.06f));
            Gizmos.DrawLine(tip, tip - dir * 0.1f - (Vector3)(Vector2.Perpendicular(dir) * 0.06f));
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        private static void DrawEnemyTargeting(EnemyTargeting targeting, GizmoType gizmoType)
        {
            var collider = targeting.GetComponent<CircleCollider2D>();
            if (collider == null)
                return;

            Handles.color = Color.cyan;
            Handles.DrawWireDisc(
                targeting.transform.position + (Vector3)collider.offset,
                Vector3.forward,
                collider.radius * Mathf.Max(targeting.transform.lossyScale.x, targeting.transform.lossyScale.y)
            );
        }
    }
}
