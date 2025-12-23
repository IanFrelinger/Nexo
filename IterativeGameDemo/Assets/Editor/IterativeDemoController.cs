using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;
using System;

namespace NexoDirectorStudio.Editor
{
    /// <summary>
    /// Controller for iterative demo that can be called from Unity Editor
    /// </summary>
    public static class IterativeDemoController
    {
        private static string GetProjectRoot()
        {
            return Application.dataPath.Replace("/Assets", "");
        }

        private static string GetScriptsPath()
        {
            return $"{GetProjectRoot()}/scripts";
        }

        /// <summary>
        /// Update assets in the Unity project for a specific iteration
        /// </summary>
        public static void UpdateIterationAssets(int iteration)
        {
            string projectRoot = GetProjectRoot();
            string iterationDir = $"Assets/Iteration{iteration}";
            
            // Create iteration folder if it doesn't exist
            if (!AssetDatabase.IsValidFolder(iterationDir))
            {
                string parentDir = "Assets";
                string[] parts = iterationDir.Split('/');
                for (int i = 1; i < parts.Length; i++)
                {
                    string currentPath = string.Join("/", parts, 0, i + 1);
                    if (!AssetDatabase.IsValidFolder(currentPath))
                    {
                        AssetDatabase.CreateFolder(parentDir, parts[i]);
                    }
                    parentDir = currentPath;
                }
            }
            
            // Create a status object in the scene
            GameObject statusObj = GameObject.Find("IterationStatus");
            if (statusObj == null)
            {
                statusObj = new GameObject("IterationStatus");
            }
            
            var statusScript = statusObj.GetComponent<IterationStatus>() ?? statusObj.AddComponent<IterationStatus>();
            statusScript.currentIteration = iteration;
            statusScript.lastUpdated = DateTime.Now;
            
            // Force refresh
            AssetDatabase.Refresh();
            EditorUtility.SetDirty(statusObj);
            
            UnityEngine.Debug.Log($"✅ Updated assets for iteration {iteration}");
        }

        /// <summary>
        /// Refresh the project to show latest iteration assets
        /// </summary>
        // Removed from menu - only Planning Chat Window is accessible
        public static void RefreshIterationAssets()
        {
            string projectRoot = GetProjectRoot();
            string artifactsPath = $"{projectRoot}/Artifacts";
            
            if (!Directory.Exists(artifactsPath))
            {
                EditorUtility.DisplayDialog("Not Found", "No artifacts folder found. Run the iterative demo first.", "OK");
                return;
            }
            
            // Find latest iteration
            var iterationDirs = Directory.GetDirectories(artifactsPath, "iteration-*");
            int maxIteration = 0;
            
            foreach (var dir in iterationDirs)
            {
                string dirName = Path.GetFileName(dir);
                if (dirName.StartsWith("iteration-"))
                {
                    string numStr = dirName.Substring("iteration-".Length);
                    if (int.TryParse(numStr, out int num) && num > maxIteration)
                    {
                        maxIteration = num;
                    }
                }
            }
            
            if (maxIteration > 0)
            {
                UpdateIterationAssets(maxIteration);
                EditorUtility.DisplayDialog("Refreshed", $"Updated assets for iteration {maxIteration}", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Not Found", "No iterations found", "OK");
            }
        }

        /// <summary>
        /// Open the planning chat window
        /// </summary>
        // Menu item removed - Planning Chat Window has its own MenuItem attribute
        public static void OpenPlanningChat()
        {
            IterativeDemoChatWindow.ShowWindow();
        }

        /// <summary>
        /// Run a single iteration from Unity Editor
        /// </summary>
        // Removed from menu - available via Planning Chat Window
        // [MenuItem("Nexo/Run Single Iteration")]
        public static void RunSingleIteration()
        {
            if (!EditorUtility.DisplayDialog("Run Iteration", 
                "This will run a single iteration of the demo. Continue?", 
                "Yes", "Cancel"))
            {
                return;
            }
            
            string scriptsPath = GetScriptsPath();
            string demoScript = $"{scriptsPath}/iterative-game-demo.sh";
            
            if (!File.Exists(demoScript))
            {
                EditorUtility.DisplayDialog("Error", $"Demo script not found at:\n{demoScript}", "OK");
                return;
            }
            
            // Run the demo script with MAX_ITERATIONS=1
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"\"{demoScript}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                EnvironmentVariables =
                {
                    ["MAX_ITERATIONS"] = "1",
                    ["OPEN_EDITOR"] = "false" // Editor is already open
                }
            };
            
            try
            {
                Process process = Process.Start(startInfo);
                process.WaitForExit();
                
                if (process.ExitCode == 0)
                {
                    RefreshIterationAssets();
                    EditorUtility.DisplayDialog("Success", "Iteration completed! Assets refreshed.", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", $"Iteration failed with exit code {process.ExitCode}", "OK");
                }
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to run iteration:\n{ex.Message}", "OK");
            }
        }
    }

    /// <summary>
    /// MonoBehaviour to track iteration status in the scene
    /// </summary>
    public class IterationStatus : MonoBehaviour
    {
        [SerializeField] public int currentIteration = 0;
        [SerializeField] public DateTime lastUpdated;
        [SerializeField] public float qualityScore = 0f;
        
        void Start()
        {
            UnityEngine.Debug.Log($"Iteration Status: Iteration {currentIteration}, Quality: {qualityScore}");
        }
    }
}

