using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NexoDirectorDemo.Editor
{
    /// <summary>
    /// Custom Unity Editor window that demonstrates Director Studio integration
    /// This window shows how to interact with the Director Studio system from Unity Editor
    /// </summary>
    public class DirectorStudioWindow : EditorWindow
    {
        private string directorToken = "";
        private bool isConnected = false;
        private Vector2 scrollPosition;
        private List<string> logMessages = new List<string>();
        private List<GateResult> gateResults = new List<GateResult>();
        
        [MenuItem("Window/Director Studio/Director Studio Control")]
        public static void ShowWindow()
        {
            GetWindow<DirectorStudioWindow>("Director Studio");
        }
        
        private void OnEnable()
        {
            // Initialize with a demo token
            directorToken = System.Guid.NewGuid().ToString();
            AddLogMessage("Director Studio window opened");
            AddLogMessage($"Generated token: {directorToken}");
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            DrawHeader();
            DrawConnectionSection();
            DrawCommandSection();
            DrawLogSection();
            DrawGateResultsSection();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Director Studio Integration Demo", EditorStyles.boldLabel);
            EditorGUILayout.Space();
        }
        
        private void DrawConnectionSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Connection", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Token:", GUILayout.Width(50));
            directorToken = EditorGUILayout.TextField(directorToken);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(isConnected ? "Disconnect" : "Connect"))
            {
                if (isConnected)
                {
                    Disconnect();
                }
                else
                {
                    Connect();
                }
            }
            
            EditorGUILayout.LabelField($"Status: {(isConnected ? "Connected" : "Disconnected")}");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawCommandSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Nexo Commands", EditorStyles.boldLabel);
            
            if (!isConnected)
            {
                EditorGUILayout.HelpBox("Connect to Director Studio to run commands", MessageType.Info);
            }
            else
            {
                if (GUILayout.Button("Run Architecture Validation"))
                {
                    RunValidationCommand();
                }
                
                if (GUILayout.Button("Run Code Analysis"))
                {
                    RunAnalysisCommand();
                }
                
                if (GUILayout.Button("List Available Agents"))
                {
                    ListAgentsCommand();
                }
                
                if (GUILayout.Button("Get Project Information"))
                {
                    GetProjectInfoCommand();
                }
                
                if (GUILayout.Button("List Scenes"))
                {
                    ListScenesCommand();
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawLogSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Log Messages", EditorStyles.boldLabel);
            
            if (logMessages.Count == 0)
            {
                EditorGUILayout.HelpBox("No log messages yet", MessageType.Info);
            }
            else
            {
                foreach (var message in logMessages)
                {
                    EditorGUILayout.LabelField(message, EditorStyles.wordWrappedLabel);
                }
            }
            
            if (GUILayout.Button("Clear Logs"))
            {
                logMessages.Clear();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawGateResultsSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Gate Results", EditorStyles.boldLabel);
            
            if (gateResults.Count == 0)
            {
                EditorGUILayout.HelpBox("No gate results yet", MessageType.Info);
            }
            else
            {
                foreach (var result in gateResults)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(result.GateName, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(result.Passed ? "✅ PASS" : "❌ FAIL", 
                        result.Passed ? EditorStyles.boldLabel : EditorStyles.boldLabel);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.LabelField(result.Message, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.Space(5);
                }
            }
            
            if (GUILayout.Button("Clear Results"))
            {
                gateResults.Clear();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void Connect()
        {
            isConnected = true;
            AddLogMessage("Connected to Director Studio");
            AddLogMessage($"Using token: {directorToken}");
        }
        
        private void Disconnect()
        {
            isConnected = false;
            AddLogMessage("Disconnected from Director Studio");
        }
        
        private void RunValidationCommand()
        {
            AddLogMessage("Running architecture validation...");
            
            // Simulate validation results
            var result = new GateResult
            {
                GateName = "Architecture Validation",
                Passed = true,
                Message = "All architecture rules passed successfully"
            };
            gateResults.Add(result);
            
            AddLogMessage("Validation completed successfully");
        }
        
        private void RunAnalysisCommand()
        {
            AddLogMessage("Running code analysis...");
            
            // Simulate analysis results
            var result = new GateResult
            {
                GateName = "Code Analysis",
                Passed = true,
                Message = "No code violations found"
            };
            gateResults.Add(result);
            
            AddLogMessage("Analysis completed successfully");
        }
        
        private void ListAgentsCommand()
        {
            AddLogMessage("Listing available agents...");
            AddLogMessage("Available agents: ArchitectureValidator, CodeAnalyzer, PerformanceMonitor");
        }
        
        private void GetProjectInfoCommand()
        {
            AddLogMessage("Getting project information...");
            AddLogMessage($"Project: {Application.productName}");
            AddLogMessage($"Unity Version: {Application.unityVersion}");
            AddLogMessage($"Platform: {Application.platform}");
        }
        
        private void ListScenesCommand()
        {
            AddLogMessage("Listing scenes...");
            int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            AddLogMessage($"Found {sceneCount} scenes in build settings");
        }
        
        private void AddLogMessage(string message)
        {
            logMessages.Add($"[{System.DateTime.Now:HH:mm:ss}] {message}");
        }
        
        [System.Serializable]
        private class GateResult
        {
            public string GateName;
            public bool Passed;
            public string Message;
        }
    }
}
