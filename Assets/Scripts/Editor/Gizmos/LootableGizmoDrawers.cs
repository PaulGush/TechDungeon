using UnityEditor;
using UnityEngine;

namespace TechDungeon.Editor
{
    public static class LootableGizmoDrawers
    {
        [DrawGizmo(GizmoType.Selected)]
        private static void DrawBounceEffect(BounceEffect bounce, GizmoType gizmoType)
        {
            var so = new SerializedObject(bounce);
            var distProp = so.FindProperty("m_bounceVerticalDistance");
            if (distProp == null)
                return;

            float dist = distProp.floatValue;
            Vector3 pos = bounce.transform.position;

            Gizmos.color = new Color(0f, 1f, 0.5f, 0.6f);

            float halfWidth = 0.3f;
            float upper = pos.y + dist;
            float lower = pos.y - dist;

            // Upper limit - dashed
            DrawDashedLine(
                new Vector3(pos.x - halfWidth, upper, pos.z),
                new Vector3(pos.x + halfWidth, upper, pos.z),
                0.05f
            );

            // Lower limit - dashed
            DrawDashedLine(
                new Vector3(pos.x - halfWidth, lower, pos.z),
                new Vector3(pos.x + halfWidth, lower, pos.z),
                0.05f
            );

            // Vertical connectors
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
            Gizmos.DrawLine(new Vector3(pos.x, upper, pos.z), new Vector3(pos.x, lower, pos.z));
        }

        private static void DrawDashedLine(Vector3 from, Vector3 to, float dashLength)
        {
            Vector3 dir = (to - from);
            float totalLength = dir.magnitude;
            dir.Normalize();
            float drawn = 0f;
            bool draw = true;

            while (drawn < totalLength)
            {
                float segEnd = Mathf.Min(drawn + dashLength, totalLength);
                if (draw)
                    Gizmos.DrawLine(from + dir * drawn, from + dir * segEnd);
                drawn = segEnd;
                draw = !draw;
            }
        }
    }
}
