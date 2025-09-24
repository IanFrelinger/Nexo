using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexoDoomGame
{
    /// <summary>
    /// Debugging and monitoring tools for Nexo Agent generation process
    /// </summary>
    public class NexoDebugger : MonoBehaviour
    {
        [Header("Debug UI")]
        [SerializeField] private TextMeshProUGUI debugLog;
        [SerializeField] private ScrollRect debugScrollRect;
        [SerializeField] private Button clearLogButton;
        [SerializeField] private Button exportLogButton;
        [SerializeField] private Toggle autoScrollToggle;
        
        [Header("Performance Monitoring")]
        [SerializeField] private TextMeshProUGUI fpsText;
        [SerializeField] private TextMeshProUGUI memoryText;
        [SerializeField] private TextMeshProUGUI cpuText;
        [SerializeField] private Slider memorySlider;
        
        [Header("Asset Monitoring")]
        [SerializeField] private TextMeshProUGUI assetCountText;
        [SerializeField] private TextMeshProUGUI scriptCountText;
        [SerializeField] private TextMeshProUGUI textureCountText;
        [SerializeField] private TextMeshProUGUI modelCountText;
        
        [Header("Generation Status")]
        [SerializeField] private TextMeshProUGUI generationStatusText;
        [SerializeField] private TextMeshProUGUI currentTaskText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Slider progressSlider;
        
        private List<string> _debugMessages = new List<string>();
        private bool _autoScroll = true;
        private float _lastFPSUpdate = 0f;
        private int _frameCount = 0;
        private float _deltaTime = 0f;
        
        private void Start()
        {
            InitializeDebugger();
            SetupUI();
        }
        
        private void Update()
        {
            UpdatePerformanceMetrics();
            UpdateAssetCounts();
        }
        
        private void InitializeDebugger()
        {
            Debug.Log("🔧 Initializing Nexo Debugger...");
            
            // Set up debug logging
            Application.logMessageReceived += OnLogMessageReceived;
            
            // Initialize performance monitoring
            _lastFPSUpdate = Time.time;
            
            LogDebug("🔧 Nexo Debugger initialized");
        }
        
        private void SetupUI()
        {
            if (clearLogButton != null)
                clearLogButton.onClick.AddListener(ClearLog);
            
            if (exportLogButton != null)
                exportLogButton.onClick.AddListener(ExportLog);
            
            if (autoScrollToggle != null)
                autoScrollToggle.onValueChanged.AddListener(OnAutoScrollChanged);
        }
        
        private void OnLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] [{type}] {logString}";
            
            _debugMessages.Add(logEntry);
            
            // Keep only last 1000 messages
            if (_debugMessages.Count > 1000)
            {
                _debugMessages.RemoveAt(0);
            }
            
            UpdateDebugDisplay();
        }
        
        private void UpdateDebugDisplay()
        {
            if (debugLog != null)
            {
                debugLog.text = string.Join("\n", _debugMessages);
                
                if (_autoScroll && debugScrollRect != null)
                {
                    Canvas.ForceUpdateCanvases();
                    debugScrollRect.verticalNormalizedPosition = 0f;
                }
            }
        }
        
        private void UpdatePerformanceMetrics()
        {
            _frameCount++;
            _deltaTime += Time.unscaledDeltaTime;
            
            if (Time.time - _lastFPSUpdate >= 1f)
            {
                float fps = _frameCount / _deltaTime;
                _frameCount = 0;
                _deltaTime = 0f;
                _lastFPSUpdate = Time.time;
                
                if (fpsText != null)
                    fpsText.text = $"FPS: {fps:F1}";
            }
            
            // Memory usage
            long memoryUsage = GC.GetTotalMemory(false);
            float memoryMB = memoryUsage / (1024f * 1024f);
            
            if (memoryText != null)
                memoryText.text = $"Memory: {memoryMB:F1} MB";
            
            if (memorySlider != null)
                memorySlider.value = memoryMB / 1000f; // Assuming 1GB max
            
            // CPU usage (simplified)
            float cpuUsage = Time.deltaTime * 1000f; // Convert to milliseconds
            if (cpuText != null)
                cpuText.text = $"Frame Time: {cpuUsage:F2} ms";
        }
        
        private void UpdateAssetCounts()
        {
            // Count assets in project
            int scriptCount = CountFilesInDirectory("Assets/Scripts", "*.cs");
            int textureCount = CountFilesInDirectory("Assets/Textures", "*.png") + 
                              CountFilesInDirectory("Assets/Textures", "*.jpg");
            int modelCount = CountFilesInDirectory("Assets/Models", "*.fbx") + 
                            CountFilesInDirectory("Assets/Models", "*.obj");
            int totalAssets = scriptCount + textureCount + modelCount;
            
            if (assetCountText != null)
                assetCountText.text = $"Total Assets: {totalAssets}";
            
            if (scriptCountText != null)
                scriptCountText.text = $"Scripts: {scriptCount}";
            
            if (textureCountText != null)
                textureCountText.text = $"Textures: {textureCount}";
            
            if (modelCountText != null)
                modelCountText.text = $"Models: {modelCount}";
        }
        
        private int CountFilesInDirectory(string directory, string pattern)
        {
            try
            {
                if (!Directory.Exists(directory))
                    return 0;
                
                return Directory.GetFiles(directory, pattern, SearchOption.AllDirectories).Length;
            }
            catch
            {
                return 0;
            }
        }
        
        public void UpdateGenerationStatus(string status, string currentTask, float progress)
        {
            if (generationStatusText != null)
                generationStatusText.text = $"Status: {status}";
            
            if (currentTaskText != null)
                currentTaskText.text = $"Current Task: {currentTask}";
            
            if (progressText != null)
                progressText.text = $"Progress: {progress:P1}";
            
            if (progressSlider != null)
                progressSlider.value = progress;
        }
        
        public void LogDebug(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] [DEBUG] {message}";
            
            _debugMessages.Add(logEntry);
            
            if (_debugMessages.Count > 1000)
            {
                _debugMessages.RemoveAt(0);
            }
            
            UpdateDebugDisplay();
            Debug.Log($"🔧 {message}");
        }
        
        public void LogInfo(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] [INFO] {message}";
            
            _debugMessages.Add(logEntry);
            
            if (_debugMessages.Count > 1000)
            {
                _debugMessages.RemoveAt(0);
            }
            
            UpdateDebugDisplay();
            Debug.Log($"ℹ️ {message}");
        }
        
        public void LogWarning(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] [WARNING] {message}";
            
            _debugMessages.Add(logEntry);
            
            if (_debugMessages.Count > 1000)
            {
                _debugMessages.RemoveAt(0);
            }
            
            UpdateDebugDisplay();
            Debug.LogWarning($"⚠️ {message}");
        }
        
        public void LogError(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] [ERROR] {message}";
            
            _debugMessages.Add(logEntry);
            
            if (_debugMessages.Count > 1000)
            {
                _debugMessages.RemoveAt(0);
            }
            
            UpdateDebugDisplay();
            Debug.LogError($"❌ {message}");
        }
        
        private void ClearLog()
        {
            _debugMessages.Clear();
            UpdateDebugDisplay();
            LogDebug("Log cleared by user");
        }
        
        private void ExportLog()
        {
            try
            {
                string logPath = Path.Combine(Application.dataPath, "NexoDebugLog.txt");
                File.WriteAllText(logPath, string.Join("\n", _debugMessages));
                LogInfo($"Debug log exported to: {logPath}");
            }
            catch (Exception ex)
            {
                LogError($"Failed to export log: {ex.Message}");
            }
        }
        
        private void OnAutoScrollChanged(bool enabled)
        {
            _autoScroll = enabled;
            LogDebug($"Auto-scroll: {(enabled ? "ON" : "OFF")}");
        }
        
        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
        }
        
        // Public methods for external access
        public void SetGenerationStatus(string status)
        {
            UpdateGenerationStatus(status, "", 0f);
        }
        
        public void SetCurrentTask(string task)
        {
            UpdateGenerationStatus("", task, 0f);
        }
        
        public void SetProgress(float progress)
        {
            UpdateGenerationStatus("", "", progress);
        }
        
        public List<string> GetDebugMessages()
        {
            return new List<string>(_debugMessages);
        }
        
        public void ClearDebugMessages()
        {
            _debugMessages.Clear();
            UpdateDebugDisplay();
        }
    }
}
