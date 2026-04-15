using System.Collections.Generic;
using Gameplay.ObjectPool;
using PlayerObject;
using UnityEditor;
using UnityEngine;
using UnityServiceLocator;

namespace TechDungeon.Editor
{
    public class QuickSpawnTool : EditorWindow
    {
        private const float PrefabCellOuterWidth = 80f;
        private const float PrefabCellInnerWidth = 72f;
        private const float PrefabButtonSize = 64f;

        private int m_selectedTab;
        private Vector2 m_scrollPosition;
        private readonly string[] m_tabNames = { "Enemy", "Weapon", "Pickup" };

        private bool m_spawnAtPlayer = true;
        private Vector2 m_customPosition;

        private readonly Dictionary<int, List<GameObject>> m_prefabs = new();
        private bool m_prefabsLoaded;

        private static readonly string[] s_searchFolders =
        {
            "Assets/Prefabs/Enemy",
            "Assets/Prefabs/Weapon",
            "Assets/Prefabs/Pickup",
        };

        [MenuItem("TechDungeon/Quick Spawn Tool")]
        public static void ShowWindow()
        {
            GetWindow<QuickSpawnTool>("Quick Spawn Tool");
        }

        private void OnEnable()
        {
            LoadPrefabs();
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            Repaint();
        }

        private void LoadPrefabs()
        {
            m_prefabs.Clear();

            for (int i = 0; i < s_searchFolders.Length; i++)
            {
                var list = new List<GameObject>();
                string folder = s_searchFolders[i];

                if (AssetDatabase.IsValidFolder(folder))
                {
                    string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
                    foreach (string guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab != null)
                            list.Add(prefab);
                    }
                }

                m_prefabs[i] = list;
            }

            // Also search Interactable/Pickups folder
            if (AssetDatabase.IsValidFolder("Assets/Prefabs/Interactable/Pickups"))
            {
                string[] pickupGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Interactable/Pickups" });
                foreach (string guid in pickupGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null && prefab.GetComponent<Pickup>() != null)
                    {
                        if (!m_prefabs[2].Contains(prefab))
                            m_prefabs[2].Add(prefab);
                    }
                }
            }

            m_prefabsLoaded = true;
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use this tool.", MessageType.Info);
                return;
            }

            if (!m_prefabsLoaded)
                LoadPrefabs();

            m_selectedTab = GUILayout.Toolbar(m_selectedTab, m_tabNames);

            EditorGUILayout.Space(4);

            // Spawn position mode
            m_spawnAtPlayer = EditorGUILayout.Toggle("Spawn At Player", m_spawnAtPlayer);
            if (!m_spawnAtPlayer)
                m_customPosition = EditorGUILayout.Vector2Field("Custom Position", m_customPosition);

            EditorGUILayout.Space(4);

            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

            if (!m_prefabs.ContainsKey(m_selectedTab) || m_prefabs[m_selectedTab].Count == 0)
            {
                EditorGUILayout.HelpBox($"No prefabs found in {s_searchFolders[m_selectedTab]}", MessageType.Warning);
            }
            else
            {
                DrawPrefabGrid(m_prefabs[m_selectedTab]);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawPrefabGrid(List<GameObject> prefabs)
        {
            int columns = Mathf.Max(1, (int)(position.width / PrefabCellOuterWidth));
            int col = 0;

            EditorGUILayout.BeginHorizontal();

            foreach (var prefab in prefabs)
            {
                if (col > 0 && col % columns == 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }

                EditorGUILayout.BeginVertical(GUILayout.Width(PrefabCellInnerWidth));

                var preview = AssetPreview.GetAssetPreview(prefab);
                var content = preview != null
                    ? new GUIContent(preview)
                    : new GUIContent(AssetPreview.GetMiniThumbnail(prefab));

                if (GUILayout.Button(content, GUILayout.Width(PrefabButtonSize), GUILayout.Height(PrefabButtonSize)))
                    SpawnPrefab(prefab);

                GUILayout.Label(prefab.name, EditorStyles.miniLabel, GUILayout.Width(PrefabButtonSize));
                EditorGUILayout.EndVertical();

                col++;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void SpawnPrefab(GameObject prefab)
        {
            Vector3 pos;

            if (m_spawnAtPlayer)
            {
                ServiceLocator.Global.TryGet(out PlayerMovementController player);
                pos = player != null ? player.transform.position : Vector3.zero;
            }
            else
            {
                pos = (Vector3)m_customPosition;
            }

            ObjectPool pool = null;
            ServiceLocator.Global.TryGet(out pool);

            GameObject spawned;
            if (pool != null)
            {
                spawned = pool.GetPooledObject(prefab);
                spawned.transform.position = pos;
            }
            else
            {
                spawned = Instantiate(prefab, pos, Quaternion.identity);
            }

            Debug.Log($"[QuickSpawn] Spawned {prefab.name} at {pos}");
        }
    }
}
