using UnityEditor;
using UnityEngine;

namespace TechDungeon.Editor
{
    [CustomEditor(typeof(AmmoSettings))]
    public class AmmoSettingsEditor : UnityEditor.Editor
    {
        private static readonly string[] s_sharedFields =
        {
            "m_Script", "BonusPierce",
            "ExplosionRadius", "ExplosionDamage", "ExplosionEffectPrefab",
            "ChainRange", "MaxChains",
            "MaxBounces"
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw shared fields (Type, DisplayName, Icon, ProjectileColor)
            DrawPropertiesExcluding(serializedObject, s_sharedFields);

            DrawIconPreview();

            var typeProp = serializedObject.FindProperty("Type");
            var ammoType = (AmmoType)typeProp.enumValueIndex;

            EditorGUILayout.Space(4);

            switch (ammoType)
            {
                case AmmoType.Piercing:
                    EditorGUILayout.LabelField("Piercing", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("BonusPierce"));
                    break;

                case AmmoType.Explosive:
                    EditorGUILayout.LabelField("Explosive", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("ExplosionRadius"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("ExplosionDamage"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("ExplosionEffectPrefab"));
                    break;

                case AmmoType.ChainLightning:
                    EditorGUILayout.LabelField("Chain Lightning", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("ChainRange"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("MaxChains"));
                    break;

                case AmmoType.Ricochet:
                    EditorGUILayout.LabelField("Ricochet", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("MaxBounces"));
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawIconPreview()
        {
            var iconProp = serializedObject.FindProperty("Icon");
            var colorProp = serializedObject.FindProperty("ProjectileColor");
            var sprite = iconProp.objectReferenceValue as Sprite;

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("HUD Preview", EditorStyles.boldLabel);

            Rect rect = GUILayoutUtility.GetRect(128, 128, GUILayout.ExpandWidth(false));
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));

            if (sprite == null)
            {
                EditorGUI.LabelField(rect, "No Icon assigned", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            Rect sr = sprite.textureRect;
            Texture tex = sprite.texture;
            Rect uv = new Rect(sr.x / tex.width, sr.y / tex.height, sr.width / tex.width, sr.height / tex.height);

            Color prev = GUI.color;
            GUI.color = colorProp.colorValue;
            GUI.DrawTextureWithTexCoords(rect, tex, uv);
            GUI.color = prev;
        }
    }
}
