using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexoDoomGame
{
    /// <summary>
    /// Orchestrates Nexo Agent tasks for Unity game generation
    /// </summary>
    public class NexoTaskOrchestrator : MonoBehaviour
    {
        [Header("Nexo Agent Configuration")]
        [SerializeField] private string configFilePath = "Assets/NexoConfig/UnityGenerationConfig.json";
        [SerializeField] private string promptsFilePath = "Assets/NexoPrompts/GameGenerationPrompts.md";
        
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Button startGenerationButton;
        [SerializeField] private Button stopGenerationButton;
        [SerializeField] private TMP_InputField customPromptInput;
        [SerializeField] private Toggle enableDebugMode;
        
        [Header("Debug Console")]
        [SerializeField] private TextMeshProUGUI debugConsole;
        [SerializeField] private ScrollRect debugScrollRect;
        
        private Nexo.Agent.Contracts.ITaskExecutionAgent _nexoAgent;
        private UnityGenerationConfig _config;
        private List<string> _generationTasks;
        private bool _isGenerating = false;
        private int _currentTaskIndex = 0;
        
        private void Start()
        {
            InitializeOrchestrator();
            SetupUI();
        }
        
        private void InitializeOrchestrator()
        {
            Debug.Log("🎮 Initializing Nexo Task Orchestrator...");
            
            try
            {
                // Load configuration
                LoadConfiguration();
                
                // Initialize Nexo Agent
                InitializeNexoAgent();
                
                // Load generation tasks
                LoadGenerationTasks();
                
                UpdateStatus("Nexo Task Orchestrator Ready");
                LogDebug("✅ Orchestrator initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to initialize orchestrator: {ex.Message}");
                LogDebug($"❌ Initialization error: {ex.Message}");
            }
        }
        
        private void LoadConfiguration()
        {
            try
            {
                string configJson = System.IO.File.ReadAllText(configFilePath);
                _config = JsonUtility.FromJson<UnityGenerationConfig>(configJson);
                LogDebug("📋 Configuration loaded successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to load configuration: {ex.Message}");
                _config = GetDefaultConfig();
            }
        }
        
        private void InitializeNexoAgent()
        {
            try
            {
                // Create Nexo Agent instance
                _nexoAgent = new Nexo.Agent.Implementations.AtlasAgent(
                    new Nexo.Agent.Implementations.SimplePlanner(),
                    new Nexo.Agent.Implementations.ToolBroker(),
                    new Nexo.Agent.Implementations.PipelineToolFactory()
                );
                
                LogDebug("🤖 Nexo Agent initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to initialize Nexo Agent: {ex.Message}");
                LogDebug($"❌ Agent initialization error: {ex.Message}");
            }
        }
        
        private void LoadGenerationTasks()
        {
            _generationTasks = new List<string>
            {
                "Generate FPS Controller script with WASD movement and mouse look",
                "Generate Weapon System script with Shotgun and Plasma Rifle",
                "Generate Enemy AI script with Imp, Demon, and Cacodemon",
                "Generate Health System script with damage feedback",
                "Generate Audio Manager script with 3D spatial audio",
                "Generate UI Manager script with retro-futuristic HUD",
                "Generate Game Manager script for level progression",
                "Generate wall textures for dark sci-fi environment",
                "Generate floor textures for industrial setting",
                "Generate weapon icons for Shotgun and Plasma Rifle",
                "Generate enemy sprites for Imp, Demon, and Cacodemon",
                "Generate UI elements for health bar and ammo counter",
                "Generate 3D models for weapons and enemies",
                "Generate audio assets for weapons, enemies, and ambient sounds",
                "Generate demo level with rooms, corridors, and lighting",
                "Generate enemy spawning and wave management system",
                "Generate performance optimization and testing systems",
                "Generate debugging and monitoring tools"
            };
            
            LogDebug($"📝 Loaded {_generationTasks.Count} generation tasks");
        }
        
        private void SetupUI()
        {
            if (startGenerationButton != null)
                startGenerationButton.onClick.AddListener(StartGeneration);
            
            if (stopGenerationButton != null)
                stopGenerationButton.onClick.AddListener(StopGeneration);
            
            if (enableDebugMode != null)
                enableDebugMode.onValueChanged.AddListener(OnDebugModeChanged);
        }
        
        public async void StartGeneration()
        {
            if (_isGenerating) return;
            
            _isGenerating = true;
            _currentTaskIndex = 0;
            
            UpdateStatus("🚀 Starting Nexo Agent generation...");
            LogDebug("🚀 Generation started");
            
            try
            {
                await ExecuteGenerationTasks();
                UpdateStatus("✅ Generation completed successfully!");
                LogDebug("✅ All generation tasks completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Generation failed: {ex.Message}");
                UpdateStatus($"❌ Generation failed: {ex.Message}");
                LogDebug($"❌ Generation error: {ex.Message}");
            }
            finally
            {
                _isGenerating = false;
            }
        }
        
        public void StopGeneration()
        {
            if (!_isGenerating) return;
            
            _isGenerating = false;
            UpdateStatus("⏹️ Generation stopped by user");
            LogDebug("⏹️ Generation stopped by user");
        }
        
        private async Task ExecuteGenerationTasks()
        {
            for (int i = 0; i < _generationTasks.Count && _isGenerating; i++)
            {
                _currentTaskIndex = i;
                var task = _generationTasks[i];
                
                UpdateStatus($"🔄 Executing task {i + 1}/{_generationTasks.Count}: {task}");
                LogDebug($"🔄 Task {i + 1}: {task}");
                
                try
                {
                    await ExecuteTask(task);
                    LogDebug($"✅ Task {i + 1} completed successfully");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Task {i + 1} failed: {ex.Message}");
                    LogDebug($"❌ Task {i + 1} error: {ex.Message}");
                }
                
                // Update progress
                float progress = (float)(i + 1) / _generationTasks.Count;
                UpdateProgress(progress);
                
                // Small delay between tasks
                await Task.Delay(500);
            }
        }
        
        private async Task ExecuteTask(string task)
        {
            try
            {
                // Create enhanced prompt with configuration
                var enhancedPrompt = CreateEnhancedPrompt(task);
                
                // Execute task with Nexo Agent
                var result = await _nexoAgent.ExecuteTaskAsync(enhancedPrompt);
                
                if (result.Success)
                {
                    LogDebug($"✅ Task result: {result.Output.Substring(0, Math.Min(100, result.Output.Length))}...");
                }
                else
                {
                    throw new Exception(result.Error ?? "Unknown error occurred");
                }
            }
            catch (Exception ex)
            {
                LogDebug($"❌ Task execution error: {ex.Message}");
                throw;
            }
        }
        
        private string CreateEnhancedPrompt(string baseTask)
        {
            var config = _config;
            var enhancedPrompt = $@"
{baseTask}

CONFIGURATION:
- Game Type: {config.gameSpecification.gameType}
- Art Style: {config.gameSpecification.artStyle}
- Color Palette: {string.Join(", ", config.gameSpecification.colorPalette)}
- Target FPS: {config.gameSpecification.targetFPS}
- Platform: {config.gameSpecification.platform}

TECHNICAL REQUIREMENTS:
- Include proper error handling and logging
- Use Unity's built-in systems
- Optimize for {config.gameSpecification.targetFPS} FPS
- Generate files in appropriate Unity directories
- Include documentation and comments

OUTPUT FORMAT:
- Generate C# scripts as .cs files
- Create Unity-compatible assets
- Include proper file organization
- Add performance optimizations
";
            
            return enhancedPrompt;
        }
        
        public async void ExecuteCustomPrompt()
        {
            if (customPromptInput == null || string.IsNullOrEmpty(customPromptInput.text))
                return;
            
            var customPrompt = customPromptInput.text;
            UpdateStatus("🔄 Executing custom prompt...");
            LogDebug($"🔄 Custom prompt: {customPrompt}");
            
            try
            {
                var enhancedPrompt = CreateEnhancedPrompt(customPrompt);
                var result = await _nexoAgent.ExecuteTaskAsync(enhancedPrompt);
                
                if (result.Success)
                {
                    UpdateStatus("✅ Custom prompt executed successfully!");
                    LogDebug($"✅ Custom prompt result: {result.Output.Substring(0, Math.Min(200, result.Output.Length))}...");
                }
                else
                {
                    UpdateStatus($"❌ Custom prompt failed: {result.Error}");
                    LogDebug($"❌ Custom prompt error: {result.Error}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Custom prompt execution failed: {ex.Message}");
                UpdateStatus($"❌ Custom prompt failed: {ex.Message}");
                LogDebug($"❌ Custom prompt error: {ex.Message}");
            }
        }
        
        private void OnDebugModeChanged(bool enabled)
        {
            if (debugConsole != null)
                debugConsole.gameObject.SetActive(enabled);
            
            if (debugScrollRect != null)
                debugScrollRect.gameObject.SetActive(enabled);
            
            LogDebug($"🔧 Debug mode: {(enabled ? "ON" : "OFF")}");
        }
        
        private void UpdateStatus(string status)
        {
            if (statusText != null)
                statusText.text = status;
            
            Debug.Log($"📊 Status: {status}");
        }
        
        private void UpdateProgress(float progress)
        {
            if (progressBar != null)
                progressBar.value = progress;
        }
        
        private void LogDebug(string message)
        {
            if (debugConsole != null)
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                debugConsole.text += $"[{timestamp}] {message}\n";
                
                // Auto-scroll to bottom
                if (debugScrollRect != null)
                {
                    Canvas.ForceUpdateCanvases();
                    debugScrollRect.verticalNormalizedPosition = 0f;
                }
            }
            
            Debug.Log($"🔧 Debug: {message}");
        }
        
        private UnityGenerationConfig GetDefaultConfig()
        {
            return new UnityGenerationConfig
            {
                nexoAgent = new NexoAgentConfig
                {
                    mode = "HYBRID",
                    enableImageGeneration = true,
                    enableCodeGeneration = true,
                    enableAssetGeneration = true,
                    enableRealTimeGeneration = true
                },
                gameSpecification = new GameSpecificationConfig
                {
                    gameType = "First-Person Shooter",
                    artStyle = "Dark Sci-Fi Horror",
                    colorPalette = new[] { "Red", "Orange", "Dark Gray" },
                    enemyTypes = new[] { "Imp", "Demon", "Cacodemon" },
                    weaponTypes = new[] { "Shotgun", "Plasma Rifle" },
                    targetFPS = 60,
                    platform = "Windows PC"
                }
            };
        }
    }
    
    /// <summary>
    /// Configuration classes for JSON serialization
    /// </summary>
    [System.Serializable]
    public class UnityGenerationConfig
    {
        public NexoAgentConfig nexoAgent;
        public GameSpecificationConfig gameSpecification;
        public GenerationSettings generationSettings;
        public OutputSettings outputSettings;
        public TestingSettings testingSettings;
        public DebuggingSettings debuggingSettings;
    }
    
    [System.Serializable]
    public class NexoAgentConfig
    {
        public string mode;
        public bool enableImageGeneration;
        public bool enableCodeGeneration;
        public bool enableAssetGeneration;
        public bool enableRealTimeGeneration;
    }
    
    [System.Serializable]
    public class GameSpecificationConfig
    {
        public string gameType;
        public string artStyle;
        public string[] colorPalette;
        public string[] enemyTypes;
        public string[] weaponTypes;
        public int targetFPS;
        public string platform;
    }
    
    [System.Serializable]
    public class GenerationSettings
    {
        public ScriptGenerationSettings scriptGeneration;
        public AssetGenerationSettings assetGeneration;
        public LevelGenerationSettings levelGeneration;
    }
    
    [System.Serializable]
    public class ScriptGenerationSettings
    {
        public bool includeComments;
        public bool includeErrorHandling;
        public bool includeLogging;
        public string codeStyle;
        public bool useInputSystem;
        public bool useNavMesh;
        public bool useAudioSystem;
    }
    
    [System.Serializable]
    public class AssetGenerationSettings
    {
        public int textureResolution;
        public string textureFormat;
        public bool generateNormalMaps;
        public bool generateSpecularMaps;
        public string audioQuality;
        public string modelDetail;
    }
    
    [System.Serializable]
    public class LevelGenerationSettings
    {
        public int roomCount;
        public int corridorCount;
        public int enemyCount;
        public int pickupCount;
        public string lightingQuality;
        public bool fogEnabled;
    }
    
    [System.Serializable]
    public class OutputSettings
    {
        public string scriptsPath;
        public string prefabsPath;
        public string scenesPath;
        public string texturesPath;
        public string modelsPath;
        public string audioPath;
        public string documentationPath;
    }
    
    [System.Serializable]
    public class TestingSettings
    {
        public bool enablePerformanceTesting;
        public bool enableGameplayTesting;
        public bool enableAudioTesting;
        public bool enableAITesting;
        public int targetFrameRate;
        public string memoryLimit;
        public string loadingTimeLimit;
    }
    
    [System.Serializable]
    public class DebuggingSettings
    {
        public bool enableDebugConsole;
        public bool enablePerformanceOverlay;
        public bool enableErrorLogging;
        public bool enableAssetValidation;
        public bool enableAIStateVisualization;
        public string logLevel;
    }
}
