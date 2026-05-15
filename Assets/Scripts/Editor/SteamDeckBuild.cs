using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TechDungeon.Editor
{
    public static class SteamDeckBuild
    {
        const string OutputDir = "Builds/SteamDeck";
        const string ExecutableName = "TechDungeon.x86_64";

        [MenuItem("TechDungeon/Build Steam Deck (Linux x64)")]
        public static void Build()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                UnityEngine.Debug.LogError("No scenes enabled in Build Settings.");
                return;
            }

            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            var outputDir = Path.Combine(projectRoot!, OutputDir);
            Directory.CreateDirectory(outputDir);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outputDir, ExecutableName),
                target = BuildTarget.StandaloneLinux64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                UnityEngine.Debug.Log(
                    $"Steam Deck build succeeded: {summary.totalSize / (1024 * 1024)} MB at {summary.outputPath}");
                EditorUtility.RevealInFinder(summary.outputPath);
            }
            else
            {
                UnityEngine.Debug.LogError($"Steam Deck build failed: {summary.result}");
            }
        }
    }
}
