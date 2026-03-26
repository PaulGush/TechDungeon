using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TechDungeon.Editor
{
    public class GameplayDashboard : EditorWindow
    {
        private int m_selectedTab;
        private Vector2 m_scrollPosition;
        private readonly string[] m_tabNames = { "Player", "Enemy", "Projectile", "Chest", "Tools" };

        private readonly Dictionary<int, List<ScriptableObject>> m_assets = new();
        private readonly Dictionary<int, List<UnityEditor.Editor>> m_editors = new();
        private readonly Dictionary<int, List<bool>> m_foldouts = new();

        private static readonly (string typeName, string createMenu, string subfolder)[] s_tabConfig =
        {
            ("PlayerSettings", "Data/Entity/Player Settings", "Player"),
            ("EnemySettings", "Data/Entity/Enemy Settings", "Enemy"),
            ("ProjectileSettings", "Data/Combat/Projectile Settings", "Projectile"),
            ("ChestSettings", "Data/Loot/Chest Settings", "Loot"),
        };

        [MenuItem("TechDungeon/Gameplay Dashboard")]
        public static void ShowWindow()
        {
            GetWindow<GameplayDashboard>("Gameplay Dashboard");
        }

        private void OnEnable()
        {
            RefreshAll();
        }

        private void OnDisable()
        {
            ClearEditors();
        }

        private void ClearEditors()
        {
            foreach (var kvp in m_editors)
                foreach (var editor in kvp.Value)
                    if (editor != null)
                        DestroyImmediate(editor);
            m_editors.Clear();
        }

        private void RefreshAll()
        {
            ClearEditors();
            m_assets.Clear();
            m_foldouts.Clear();

            for (int i = 0; i < s_tabConfig.Length; i++)
            {
                var assets = new List<ScriptableObject>();
                var editors = new List<UnityEditor.Editor>();
                var foldouts = new List<bool>();

                string[] guids = AssetDatabase.FindAssets($"t:{s_tabConfig[i].typeName}");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                    if (asset != null)
                    {
                        assets.Add(asset);
                        editors.Add(UnityEditor.Editor.CreateEditor(asset));
                        foldouts.Add(false);
                    }
                }

                m_assets[i] = assets;
                m_editors[i] = editors;
                m_foldouts[i] = foldouts;
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            m_selectedTab = GUILayout.Toolbar(m_selectedTab, m_tabNames, EditorStyles.toolbarButton);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
                RefreshAll();
            EditorGUILayout.EndHorizontal();

            if (m_selectedTab == m_tabNames.Length - 1)
            {
                DrawToolsTab();
                return;
            }

            if (!m_assets.ContainsKey(m_selectedTab))
                return;

            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

            var assets = m_assets[m_selectedTab];
            var editors = m_editors[m_selectedTab];
            var foldouts = m_foldouts[m_selectedTab];

            if (assets.Count == 0)
            {
                EditorGUILayout.HelpBox($"No {m_tabNames[m_selectedTab]} settings assets found.", MessageType.Info);
            }

            for (int i = 0; i < assets.Count; i++)
            {
                if (assets[i] == null) continue;

                EditorGUILayout.BeginHorizontal();
                foldouts[i] = EditorGUILayout.Foldout(foldouts[i], assets[i].name, true, EditorStyles.foldoutHeader);
                if (GUILayout.Button("Ping", GUILayout.Width(40)))
                    EditorGUIUtility.PingObject(assets[i]);
                EditorGUILayout.EndHorizontal();

                if (foldouts[i] && editors[i] != null)
                {
                    EditorGUI.indentLevel++;
                    editors[i].OnInspectorGUI();
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space(4);
                }
            }

            EditorGUILayout.Space(8);

            if (GUILayout.Button($"Create New {m_tabNames[m_selectedTab]} Settings"))
                CreateNewAsset(m_selectedTab);

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolsTab()
        {
            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

            EditorGUILayout.LabelField("Room", EditorStyles.boldLabel);

            bool isPlaying = Application.isPlaying;
            if (!isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use room tools.", MessageType.Info);
            }

            EditorGUI.BeginDisabledGroup(!isPlaying);

            if (GUILayout.Button("Clear Current Room"))
            {
                var room = FindAnyObjectByType<RoomInstance>();
                if (room != null)
                {
                    var encounter = room.GetComponent<RoomEncounter>();
                    if (encounter != null)
                    {
                        encounter.KillAllEnemies();
                    }
                    else
                    {
                        room.ClearRoom();
                    }

                    Debug.Log($"[GameplayDashboard] Cleared room: {room.name}");
                }
                else
                {
                    Debug.LogWarning("[GameplayDashboard] No active RoomInstance found in scene.");
                }
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndScrollView();
        }

        private void CreateNewAsset(int tabIndex)
        {
            var config = s_tabConfig[tabIndex];
            string folder = $"Assets/Data/{config.subfolder}";

            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/Data", config.subfolder);

            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/New{config.typeName}.asset");

            Type type = UnityEditor.TypeCache.GetTypesDerivedFrom<ScriptableObject>()
                .FirstOrDefault(t => t.Name == config.typeName);

            if (type == null)
            {
                // Fallback: search non-derived types
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = asm.GetType(config.typeName);
                    if (type != null) break;
                }
            }

            if (type == null)
            {
                Debug.LogError($"Could not find type: {config.typeName}");
                return;
            }

            var asset = CreateInstance(type);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
            RefreshAll();
        }

    }
}
