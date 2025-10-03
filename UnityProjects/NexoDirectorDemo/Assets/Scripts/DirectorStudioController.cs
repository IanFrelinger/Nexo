using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NexoDirectorDemo
{
    /// <summary>
    /// Controller script that demonstrates Director Studio integration
    /// This script shows how to interact with the Director Studio system
    /// </summary>
    public class DirectorStudioController : MonoBehaviour
    {
        [Header("Director Studio Integration")]
        [SerializeField] private bool enableDirectorStudio = true;
        [SerializeField] private string directorToken = "";
        [SerializeField] private bool isConnectedToDirector = false;
        
        [Header("Nexo Commands")]
        [SerializeField] private List<string> availableCommands = new List<string>();
        [SerializeField] private string lastCommandResult = "";
        
        [Header("Real-time Status")]
        [SerializeField] private bool isPlaying = false;
        [SerializeField] private int sceneCount = 0;
        [SerializeField] private string projectInfo = "";
        
        private void Start()
        {
            InitializeDirectorStudio();
            LoadAvailableCommands();
            UpdateProjectInfo();
        }
        
        private void InitializeDirectorStudio()
        {
            if (enableDirectorStudio)
            {
                Debug.Log("Director Studio integration enabled");
                // In a real implementation, this would connect to the Director Studio
                // For demo purposes, we'll simulate the connection
                isConnectedToDirector = true;
                directorToken = System.Guid.NewGuid().ToString();
                Debug.Log($"Director Studio token: {directorToken}");
            }
        }
        
        private void LoadAvailableCommands()
        {
            availableCommands.Clear();
            availableCommands.Add("validate --filter Category=Architecture");
            availableCommands.Add("analyze --format-json");
            availableCommands.Add("agent --name list");
            availableCommands.Add("agent --name analyze");
        }
        
        private void UpdateProjectInfo()
        {
            projectInfo = $"Project: {Application.productName}\n" +
                         $"Version: {Application.version}\n" +
                         $"Unity Version: {Application.unityVersion}\n" +
                         $"Platform: {Application.platform}\n" +
                         $"Data Path: {Application.dataPath}";
            
            sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
        }
        
        /// <summary>
        /// Simulates running a Nexo validation command
        /// </summary>
        public void RunValidationCommand()
        {
            if (!isConnectedToDirector)
            {
                Debug.LogWarning("Not connected to Director Studio");
                return;
            }
            
            Debug.Log("Running Nexo validation command...");
            // Simulate command execution
            lastCommandResult = "Validation completed successfully - All architecture rules passed";
            Debug.Log($"Result: {lastCommandResult}");
        }
        
        /// <summary>
        /// Simulates running a Nexo analysis command
        /// </summary>
        public void RunAnalysisCommand()
        {
            if (!isConnectedToDirector)
            {
                Debug.LogWarning("Not connected to Director Studio");
                return;
            }
            
            Debug.Log("Running Nexo analysis command...");
            // Simulate command execution
            lastCommandResult = "Analysis completed - No violations found";
            Debug.Log($"Result: {lastCommandResult}");
        }
        
        /// <summary>
        /// Simulates listing available agents
        /// </summary>
        public void ListAgentsCommand()
        {
            if (!isConnectedToDirector)
            {
                Debug.LogWarning("Not connected to Director Studio");
                return;
            }
            
            Debug.Log("Listing available agents...");
            // Simulate command execution
            lastCommandResult = "Available agents: ArchitectureValidator, CodeAnalyzer, PerformanceMonitor";
            Debug.Log($"Result: {lastCommandResult}");
        }
        
        /// <summary>
        /// Toggles play mode (simulates Unity Editor control)
        /// </summary>
        public void TogglePlayMode()
        {
            isPlaying = !isPlaying;
            Debug.Log($"Play mode: {(isPlaying ? "ON" : "OFF")}");
        }
        
        /// <summary>
        /// Gets current project information
        /// </summary>
        public string GetProjectInfo()
        {
            UpdateProjectInfo();
            return projectInfo;
        }
        
        /// <summary>
        /// Gets list of scenes in the project
        /// </summary>
        public List<string> GetSceneList()
        {
            var scenes = new List<string>();
            for (int i = 0; i < sceneCount; i++)
            {
                scenes.Add($"Scene {i + 1}");
            }
            return scenes;
        }
        
        private void OnGUI()
        {
            if (!enableDirectorStudio) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("Director Studio Demo", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            GUILayout.Label($"Status: {(isConnectedToDirector ? "Connected" : "Disconnected")}");
            GUILayout.Label($"Token: {directorToken}");
            GUILayout.Space(10);
            
            if (GUILayout.Button("Run Validation"))
            {
                RunValidationCommand();
            }
            
            if (GUILayout.Button("Run Analysis"))
            {
                RunAnalysisCommand();
            }
            
            if (GUILayout.Button("List Agents"))
            {
                ListAgentsCommand();
            }
            
            if (GUILayout.Button($"Toggle Play Mode ({(isPlaying ? "ON" : "OFF")})"))
            {
                TogglePlayMode();
            }
            
            GUILayout.Space(10);
            GUILayout.Label("Last Result:");
            GUILayout.TextArea(lastCommandResult, GUILayout.Height(60));
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
