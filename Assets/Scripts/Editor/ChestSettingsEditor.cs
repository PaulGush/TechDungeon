using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TechDungeon.Editor
{
    [CustomEditor(typeof(ChestSettings))]
    public class ChestSettingsEditor : UnityEditor.Editor
    {
        private string m_simulationResult;
        private GUIStyle m_barLabelStyleLight;
        private GUIStyle m_barLabelStyleDark;

        private void InitStyles()
        {
            if (m_barLabelStyleLight != null) return;
            m_barLabelStyleLight = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            m_barLabelStyleDark = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.black }
            };
        }

        public override void OnInspectorGUI()
        {
            InitStyles();
            serializedObject.Update();

            var settings = (ChestSettings)target;

            // Draw properties before Guaranteed Drops header
            DrawPropertiesExcluding(serializedObject,
                "m_Script",
                "GuaranteedItems",
                "GuaranteedTypes",
                "TotalSpawnTime",
                "SpawnTimeInterval");

            // Warn if no database assigned
            if (settings.LootDatabase == null)
            {
                EditorGUILayout.HelpBox(
                    "No Loot Database assigned. This chest will not drop any items.",
                    MessageType.Error);
            }

            // Warn about empty categories
            if (settings.ItemCategories == null || settings.ItemCategories.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No item categories selected. Add at least one category for random drops.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(4);

            // Guaranteed Drops section with validation
            int maxGuaranteed = settings.ItemDropCount;
            int currentGuaranteed = settings.TotalGuaranteedCount;

            EditorGUILayout.LabelField("Guaranteed Drops", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"Using {currentGuaranteed} of {maxGuaranteed} guaranteed slots. " +
                $"Remaining {Mathf.Max(0, maxGuaranteed - currentGuaranteed)} slots will be filled randomly from allowed categories.",
                currentGuaranteed > maxGuaranteed ? MessageType.Error : MessageType.Info);

            if (currentGuaranteed > maxGuaranteed)
            {
                EditorGUILayout.HelpBox(
                    $"Too many guaranteed drops! You have {currentGuaranteed} guaranteed but only {maxGuaranteed} items will spawn. " +
                    $"Remove {currentGuaranteed - maxGuaranteed} guaranteed entry(s).",
                    MessageType.Error);
            }

            SerializedProperty guaranteedItemsProp = serializedObject.FindProperty("GuaranteedItems");
            SerializedProperty guaranteedTypesProp = serializedObject.FindProperty("GuaranteedTypes");

            EditorGUILayout.PropertyField(guaranteedItemsProp, new GUIContent("Guaranteed Items", "Specific prefabs that will always drop."), true);
            EditorGUILayout.PropertyField(guaranteedTypesProp, new GUIContent("Guaranteed Types", "Item types — a random item of each type will drop from the loot database."), true);

            // Check for missing type coverage in the database
            if (settings.LootDatabase != null && settings.GuaranteedTypes != null)
            {
                foreach (LootItemType type in settings.GuaranteedTypes)
                {
                    if (!settings.LootDatabase.HasItemsOfType(type))
                    {
                        EditorGUILayout.HelpBox(
                            $"No items of type \"{type}\" found in the Loot Database. This guaranteed type will be skipped.",
                            MessageType.Warning);
                    }
                }
            }

            // Check category coverage in database
            if (settings.LootDatabase != null && settings.ItemCategories != null)
            {
                foreach (LootItemType type in settings.ItemCategories)
                {
                    if (!settings.LootDatabase.HasItemsOfType(type))
                    {
                        EditorGUILayout.HelpBox(
                            $"No items of type \"{type}\" found in the Loot Database. This category will produce no drops.",
                            MessageType.Warning);
                    }
                }
            }

            EditorGUILayout.Space(4);

            // Spawn timing
            EditorGUILayout.PropertyField(serializedObject.FindProperty("TotalSpawnTime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SpawnTimeInterval"));

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8);

            // Validation
            float totalChance = settings.LegendaryDropChance + settings.EpicDropChance + settings.RareDropChance + settings.UncommonDropChance;
            if (totalChance > 100f)
            {
                EditorGUILayout.HelpBox(
                    $"Drop chances sum to {totalChance:F1}% which exceeds 100%. Common items will never drop.",
                    MessageType.Warning);
            }

            // Rarity distribution bar
            EditorGUILayout.LabelField("Rarity Distribution", EditorStyles.boldLabel);
            float commonChance = Mathf.Max(0f, 100f - totalChance);

            Rect barRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(24));
            barRect = EditorGUI.IndentedRect(barRect);

            DrawRarityBar(barRect, commonChance, settings.UncommonDropChance, settings.RareDropChance, settings.EpicDropChance, settings.LegendaryDropChance);

            // Item preview from database for selected categories
            if (settings.LootDatabase != null && settings.ItemCategories != null && settings.ItemCategories.Count > 0)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Available Items (from Database)", EditorStyles.boldLabel);

                int columns = 4;
                int row = 0;
                EditorGUILayout.BeginHorizontal();

                foreach (LootItemType category in settings.ItemCategories)
                {
                    var items = settings.LootDatabase.GetItemsOfType(category);
                    foreach (Lootable item in items)
                    {
                        if (item == null) continue;

                        if (row > 0 && row % columns == 0)
                        {
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.BeginHorizontal();
                        }

                        EditorGUILayout.BeginVertical(GUILayout.Width(72));
                        var preview = AssetPreview.GetAssetPreview(item.gameObject);
                        if (preview != null)
                            GUILayout.Label(preview, GUILayout.Width(64), GUILayout.Height(64));
                        else
                            GUILayout.Label(AssetPreview.GetMiniThumbnail(item.gameObject), GUILayout.Width(64), GUILayout.Height(64));

                        string typeSuffix = $" [{item.ItemType}]";
                        GUILayout.Label(item.name + typeSuffix, EditorStyles.miniLabel, GUILayout.Width(64));
                        EditorGUILayout.EndVertical();

                        row++;
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(4);

            // Simulate button
            if (GUILayout.Button("Simulate 1000 Drops"))
            {
                m_simulationResult = RunSimulation(settings);
            }

            if (!string.IsNullOrEmpty(m_simulationResult))
            {
                EditorGUILayout.HelpBox(m_simulationResult, MessageType.Info);
            }
        }

        private void DrawRarityBar(Rect rect, float common, float uncommon, float rare, float epic, float legendary)
        {
            float total = common + uncommon + rare + epic + legendary;
            if (total <= 0) return;

            var segments = new (float pct, Color color, string label)[]
            {
                (common, LootableRarity.RarityColors[LootableRarity.Rarity.Common], "Common"),
                (uncommon, LootableRarity.RarityColors[LootableRarity.Rarity.Uncommon], "Uncommon"),
                (rare, LootableRarity.RarityColors[LootableRarity.Rarity.Rare], "Rare"),
                (epic, LootableRarity.RarityColors[LootableRarity.Rarity.Epic], "Epic"),
                (legendary, LootableRarity.RarityColors[LootableRarity.Rarity.Legendary], "Legendary"),
            };

            float x = rect.x;
            foreach (var (pct, color, label) in segments)
            {
                if (pct <= 0) continue;
                float width = (pct / total) * rect.width;
                Rect segRect = new Rect(x, rect.y, width, rect.height);
                EditorGUI.DrawRect(segRect, color);

                if (width > 30)
                {
                    var style = (color.r + color.g + color.b > 1.5f) ? m_barLabelStyleDark : m_barLabelStyleLight;
                    GUI.Label(segRect, $"{pct:F0}%", style);
                }

                x += width;
            }
        }

        private string RunSimulation(ChestSettings settings)
        {
            var counts = new Dictionary<LootableRarity.Rarity, int>
            {
                { LootableRarity.Rarity.Common, 0 },
                { LootableRarity.Rarity.Uncommon, 0 },
                { LootableRarity.Rarity.Rare, 0 },
                { LootableRarity.Rarity.Epic, 0 },
                { LootableRarity.Rarity.Legendary, 0 },
            };

            for (int i = 0; i < 1000; i++)
            {
                var rarity = LootableRarity.DetermineRarity(
                    settings.LegendaryDropChance, settings.EpicDropChance, settings.RareDropChance, settings.UncommonDropChance);
                counts[rarity]++;
            }

            float commonExpected = Mathf.Max(0, 100 - settings.LegendaryDropChance - settings.EpicDropChance - settings.RareDropChance - settings.UncommonDropChance);

            return $"Simulation Results (1000 drops):\n" +
                   $"Common:    {counts[LootableRarity.Rarity.Common] / 10f:F1}% (configured: {commonExpected:F1}%)\n" +
                   $"Uncommon:  {counts[LootableRarity.Rarity.Uncommon] / 10f:F1}% (configured: {settings.UncommonDropChance:F1}%)\n" +
                   $"Rare:      {counts[LootableRarity.Rarity.Rare] / 10f:F1}% (configured: {settings.RareDropChance:F1}%)\n" +
                   $"Epic:      {counts[LootableRarity.Rarity.Epic] / 10f:F1}% (configured: {settings.EpicDropChance:F1}%)\n" +
                   $"Legendary: {counts[LootableRarity.Rarity.Legendary] / 10f:F1}% (configured: {settings.LegendaryDropChance:F1}%)";
        }
    }
}
