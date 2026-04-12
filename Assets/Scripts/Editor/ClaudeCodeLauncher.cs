using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace TechDungeon.Editor
{
    public static class ClaudeCodeLauncher
    {
        [MenuItem("TechDungeon/Open Claude Code")]
        public static void OpenClaudeCode()
        {
            var projectPath = Application.dataPath.Replace("/Assets", "");

            if (Application.platform == RuntimePlatform.LinuxEditor)
            {
                TryLaunchLinuxTerminal(projectPath);
            }
            else if (Application.platform == RuntimePlatform.OSXEditor)
            {
                var script = $"tell application \"Terminal\" to do script \"cd '{projectPath}' && claude\"";
                Process.Start("osascript", $"-e '{script}'");
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c start cmd /k \"cd /d \"{projectPath}\" && claude\"",
                    UseShellExecute = false,
                };
                Process.Start(startInfo);
            }

            UnityEngine.Debug.Log($"[ClaudeCode] Launching Claude Code in: {projectPath}");
        }

        static void TryLaunchLinuxTerminal(string projectPath)
        {
            string[] terminals =
            {
                "gnome-terminal", "konsole", "xfce4-terminal",
                "mate-terminal", "xterm",
            };

            foreach (var terminal in terminals)
            {
                try
                {
                    var startInfo = terminal switch
                    {
                        "gnome-terminal" => new ProcessStartInfo
                        {
                            FileName = terminal,
                            Arguments = $"-- bash -c 'cd \"{projectPath}\" && claude; exec bash'",
                            UseShellExecute = false,
                        },
                        "konsole" => new ProcessStartInfo
                        {
                            FileName = terminal,
                            Arguments = $"-e bash -c 'cd \"{projectPath}\" && claude; exec bash'",
                            UseShellExecute = false,
                        },
                        _ => new ProcessStartInfo
                        {
                            FileName = terminal,
                            Arguments = $"-e bash -c 'cd \"{projectPath}\" && claude; exec bash'",
                            UseShellExecute = false,
                        },
                    };

                    Process.Start(startInfo);
                    return;
                }
                catch
                {
                    // Terminal not found, try next one
                }
            }

            UnityEngine.Debug.LogError("[ClaudeCode] No supported terminal emulator found.");
        }
    }
}
