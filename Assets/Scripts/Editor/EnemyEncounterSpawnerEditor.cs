using UnityEditor;
using UnityEngine;

namespace TechDungeon.Editor
{
    [CustomEditor(typeof(EnemyEncounterSpawner))]
    public class EnemyEncounterSpawnerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var spawner = (EnemyEncounterSpawner)target;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Info", EditorStyles.boldLabel);

            // Spawn duration summary
            float totalDuration = spawner.SpawnCount > 1
                ? (spawner.SpawnCount - 1) * spawner.DelayBetweenSpawns
                : 0f;
            EditorGUILayout.LabelField("Total Spawn Duration", $"{totalDuration:F2}s");

            // Per-entry spawn probability
            int totalWeight = 0;
            foreach (var entry in spawner.SpawnEntries)
                totalWeight += entry.Weight;

            if (totalWeight > 0 && spawner.SpawnEntries.Count > 0)
            {
                EditorGUILayout.LabelField("Spawn Probabilities", EditorStyles.boldLabel);
                foreach (var entry in spawner.SpawnEntries)
                {
                    string name = entry.EnemyPrefab != null ? entry.EnemyPrefab.name : "(none)";
                    float pct = (entry.Weight / (float)totalWeight) * 100f;
                    EditorGUILayout.LabelField($"  {name}", $"{pct:F1}%");
                }
            }

            EditorGUILayout.Space(4);

            if (GUILayout.Button("Add Spawn Point"))
            {
                var child = new GameObject($"SpawnPoint_{spawner.CustomSpawnPoints.Count}");
                child.transform.SetParent(spawner.transform);
                Vector2 offset = Random.insideUnitCircle * spawner.SpawnRadius;
                child.transform.localPosition = (Vector3)offset;

                Undo.RegisterCreatedObjectUndo(child, "Add Spawn Point");

                var prop = serializedObject.FindProperty("m_customSpawnPoints");
                serializedObject.Update();
                prop.arraySize++;
                prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = child.transform;
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void OnSceneGUI()
        {
            var spawner = (EnemyEncounterSpawner)target;
            Vector3 center = spawner.transform.position;

            // Trigger radius - yellow
            Handles.color = new Color(1f, 1f, 0f, 0.08f);
            Handles.DrawSolidDisc(center, Vector3.forward, spawner.TriggerRadius);
            Handles.color = new Color(1f, 1f, 0f, 0.5f);
            Handles.DrawWireDisc(center, Vector3.forward, spawner.TriggerRadius);

            EditorGUI.BeginChangeCheck();
            float newTrigger = Handles.RadiusHandle(Quaternion.identity, center, spawner.TriggerRadius);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(spawner, "Resize Trigger Radius");
                serializedObject.FindProperty("m_triggerRadius").floatValue = newTrigger;
                serializedObject.ApplyModifiedProperties();
            }

            // Spawn radius - red
            Handles.color = new Color(1f, 0f, 0f, 0.08f);
            Handles.DrawSolidDisc(center, Vector3.forward, spawner.SpawnRadius);
            Handles.color = new Color(1f, 0f, 0f, 0.5f);
            Handles.DrawWireDisc(center, Vector3.forward, spawner.SpawnRadius);

            EditorGUI.BeginChangeCheck();
            float newSpawn = Handles.RadiusHandle(Quaternion.identity, center, spawner.SpawnRadius);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(spawner, "Resize Spawn Radius");
                serializedObject.FindProperty("m_spawnRadius").floatValue = newSpawn;
                serializedObject.ApplyModifiedProperties();
            }

            // Custom spawn points
            Handles.color = Color.magenta;
            foreach (var point in spawner.CustomSpawnPoints)
            {
                if (point == null) continue;
                Handles.DrawSolidDisc(point.position, Vector3.forward, 0.15f);
                Handles.DrawDottedLine(center, point.position, 4f);
            }

            // Labels
            Handles.color = Color.yellow;
            Handles.Label(center + Vector3.up * (spawner.TriggerRadius + 0.3f), "Trigger");
            Handles.color = Color.red;
            Handles.Label(center + Vector3.up * (spawner.SpawnRadius + 0.3f), "Spawn");
        }
    }
}
