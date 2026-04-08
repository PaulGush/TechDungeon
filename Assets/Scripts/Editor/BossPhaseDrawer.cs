using UnityEditor;
using UnityEngine;

namespace TechDungeon.Editor
{
    [CustomEditor(typeof(BossSettings))]
    public class BossSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw all EnemySettings base fields
            DrawPropertiesExcluding(serializedObject, "m_Script", "Phases");

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Boss Phases", EditorStyles.boldLabel);

            SerializedProperty phases = serializedObject.FindProperty("Phases");
            phases.arraySize = EditorGUILayout.IntField("Phases", phases.arraySize);

            for (int i = 0; i < phases.arraySize; i++)
            {
                SerializedProperty phase = phases.GetArrayElementAtIndex(i);
                phase.isExpanded = EditorGUILayout.Foldout(phase.isExpanded, $"Phase {i}", true);

                if (!phase.isExpanded) continue;

                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(phase.FindPropertyRelative("HealthThreshold"));

                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Attack", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(phase.FindPropertyRelative("AttackType"));
                EditorGUILayout.PropertyField(phase.FindPropertyRelative("FireRateOverride"));

                var attackType = (BossAttackType)phase.FindPropertyRelative("AttackType").enumValueIndex;

                if (attackType == BossAttackType.Flamethrower)
                {
                    EditorGUILayout.Space(2);
                    EditorGUILayout.LabelField("Flamethrower", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(phase.FindPropertyRelative("FlameDuration"));
                    EditorGUILayout.PropertyField(phase.FindPropertyRelative("FlameDamagePerTick"));
                    EditorGUILayout.PropertyField(phase.FindPropertyRelative("FlameTickInterval"));
                }

                if (attackType == BossAttackType.Burst)
                {
                    EditorGUILayout.Space(2);
                    EditorGUILayout.LabelField("Burst", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(phase.FindPropertyRelative("BurstProjectileCount"));
                    EditorGUILayout.PropertyField(phase.FindPropertyRelative("BurstRadius"));
                }

                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Movement", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(phase.FindPropertyRelative("SpeedOverride"));
                EditorGUILayout.PropertyField(phase.FindPropertyRelative("AggressiveChase"));

                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Minions", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(phase.FindPropertyRelative("SummonsMinions"));

                if (phase.FindPropertyRelative("SummonsMinions").boolValue)
                {
                    EditorGUILayout.PropertyField(phase.FindPropertyRelative("MinionCount"));
                    EditorGUILayout.PropertyField(phase.FindPropertyRelative("MinionPrefab"));
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.Space(4);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
