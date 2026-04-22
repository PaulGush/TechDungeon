using UnityEditor;
using UnityEngine;

namespace TechDungeon.Editor
{
    [CustomEditor(typeof(Mutation))]
    public class MutationEditor : UnityEditor.Editor
    {
        private const float PreviewSize = 128f;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Mutation mutation = (Mutation)target;
            Sprite icon = mutation.Icon;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Icon Preview", EditorStyles.boldLabel);

            Rect rect = GUILayoutUtility.GetRect(PreviewSize, PreviewSize, GUILayout.ExpandWidth(false));
            if (icon == null || icon.texture == null)
            {
                EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));
                GUI.Label(rect, "(no icon)", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            Texture2D tex = icon.texture;
            Rect texRect = icon.textureRect;
            Rect texCoords = new Rect(
                texRect.x / tex.width,
                texRect.y / tex.height,
                texRect.width / tex.width,
                texRect.height / tex.height);

            EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 1f));
            GUI.DrawTextureWithTexCoords(rect, tex, texCoords);
        }
    }
}
