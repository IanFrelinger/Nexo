using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Text.RegularExpressions;

namespace NexoDirectorStudio.Editor
{
    /// <summary>
    /// Unity Editor window for viewing and controlling iterative game development
    /// </summary>
    public class IterativeDemoWindow : EditorWindow
    {
        private string projectRoot = "";
        private int currentIteration = 0;
        private float currentQualityScore = 0f;
        private float targetQualityScore = 7.0f;
        private string statusMessage = "Ready";
        private Vector2 scrollPosition;
        private bool autoRefresh = true;
        private double lastRefreshTime = 0;
        private const double REFRESH_INTERVAL = 2.0; // Refresh every 2 seconds

        // Removed - Planning Chat Window is now the primary interface
        // This window is kept for internal use but not exposed in the menu

        private void OnEnable()
        {
            // Find project root (parent of Assets folder)
            projectRoot = Application.dataPath.Replace("/Assets", "");
            
            // Load current status
            RefreshStatus();
            
            // Subscribe to asset refresh events
            EditorApplication.projectChanged += OnProjectChanged;
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= OnProjectChanged;
        }

        private void OnProjectChanged()
        {
            if (autoRefresh)
            {
                RefreshStatus();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            // Header
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 16;
            EditorGUILayout.LabelField("🔄 Iterative Game Development", headerStyle);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("Project Root:", projectRoot);
            EditorGUILayout.Space(10);
            
            // Status section
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Current Status:", statusMessage);
            EditorGUILayout.IntField("Current Iteration:", currentIteration);
            EditorGUILayout.FloatField("Quality Score:", currentQualityScore);
            EditorGUILayout.FloatField("Target Score:", targetQualityScore);
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space(10);
            
            // Progress bar
            if (targetQualityScore > 0)
            {
                float progress = Mathf.Clamp01(currentQualityScore / targetQualityScore);
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progress, 
                    $"Quality: {currentQualityScore:F1} / {targetQualityScore:F1}");
            }
            
            EditorGUILayout.Space(10);
            
            // Controls
            EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔄 Refresh Status"))
            {
                RefreshStatus();
            }
            
            autoRefresh = EditorGUILayout.Toggle("Auto Refresh", autoRefresh);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("📁 Open Iteration Folder"))
            {
                string iterationPath = $"{projectRoot}/Artifacts/iteration-{currentIteration}";
                if (Directory.Exists(iterationPath))
                {
                    EditorUtility.RevealInFinder(iterationPath);
                }
                else
                {
                    EditorUtility.DisplayDialog("Not Found", 
                        $"Iteration {currentIteration} folder not found at:\n{iterationPath}", "OK");
                }
            }
            
            EditorGUILayout.Space(10);
            
            // Recent iterations
            EditorGUILayout.LabelField("Recent Iterations", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // Find all iteration folders
            string artifactsPath = $"{projectRoot}/Artifacts";
            if (Directory.Exists(artifactsPath))
            {
                var iterationDirs = Directory.GetDirectories(artifactsPath, "iteration-*");
                Array.Sort(iterationDirs, (a, b) => 
                {
                    int numA = ExtractIterationNumber(a);
                    int numB = ExtractIterationNumber(b);
                    return numB.CompareTo(numA); // Descending order
                });
                
                foreach (var dir in iterationDirs)
                {
                    int iterNum = ExtractIterationNumber(dir);
                    string playtestFile = $"{dir}/iteration-{iterNum}-playtest-results.json";
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Iteration {iterNum}:", GUILayout.Width(100));
                    
                    if (File.Exists(playtestFile))
                    {
                        try
                        {
                            float quality = ParseQualityFromJson(playtestFile);
                            if (quality > 0)
                            {
                                EditorGUILayout.LabelField($"Quality: {quality:F1}", GUILayout.Width(100));
                            }
                        }
                        catch { }
                    }
                    
                    if (GUILayout.Button("View", GUILayout.Width(60)))
                    {
                        EditorUtility.RevealInFinder(dir);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.LabelField("No iterations found yet");
            }
            
            EditorGUILayout.EndScrollView();
            
            // Auto-refresh logic
            if (autoRefresh && EditorApplication.timeSinceStartup - lastRefreshTime > REFRESH_INTERVAL)
            {
                RefreshStatus();
                lastRefreshTime = EditorApplication.timeSinceStartup;
            }
        }

        private void RefreshStatus()
        {
            // Find latest iteration
            string artifactsPath = $"{projectRoot}/Artifacts";
            if (Directory.Exists(artifactsPath))
            {
                var iterationDirs = Directory.GetDirectories(artifactsPath, "iteration-*");
                int maxIteration = 0;
                
                foreach (var dir in iterationDirs)
                {
                    int iterNum = ExtractIterationNumber(dir);
                    if (iterNum > maxIteration)
                    {
                        maxIteration = iterNum;
                    }
                }
                
                currentIteration = maxIteration;
                
                // Load playtest results for latest iteration
                string playtestFile = $"{artifactsPath}/iteration-{currentIteration}/iteration-{currentIteration}-playtest-results.json";
                if (File.Exists(playtestFile))
                {
                    try
                    {
                        currentQualityScore = ParseQualityFromJson(playtestFile);
                        statusMessage = ParseStatusFromJson(playtestFile);
                    }
                    catch (Exception ex)
                    {
                        statusMessage = $"Error: {ex.Message}";
                    }
                }
                else
                {
                    statusMessage = "No results yet";
                }
            }
            else
            {
                currentIteration = 0;
                statusMessage = "No iterations started";
            }
            
            Repaint();
        }

        private float ParseQualityFromJson(string jsonPath)
        {
            try
            {
                string json = File.ReadAllText(jsonPath);
                
                // Simple regex to extract overallQuality from metrics
                // Look for "overallQuality": 5.0 or "overallQuality":5.0
                var match = Regex.Match(json, @"""overallQuality""\s*:\s*([0-9]+\.?[0-9]*)");
                if (match.Success && match.Groups.Count > 1)
                {
                    if (float.TryParse(match.Groups[1].Value, out float quality))
                    {
                        return quality;
                    }
                }
            }
            catch { }
            
            return 0f;
        }

        private string ParseStatusFromJson(string jsonPath)
        {
            try
            {
                string json = File.ReadAllText(jsonPath);
                
                // Extract status field
                var match = Regex.Match(json, @"""status""\s*:\s*""([^""]+)""");
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value;
                }
            }
            catch { }
            
            return "Completed";
        }

        private int ExtractIterationNumber(string path)
        {
            string dirName = Path.GetFileName(path);
            if (dirName.StartsWith("iteration-"))
            {
                string numStr = dirName.Substring("iteration-".Length);
                if (int.TryParse(numStr, out int num))
                {
                    return num;
                }
            }
            return 0;
        }
    }
}
