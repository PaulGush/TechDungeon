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
    }
}
