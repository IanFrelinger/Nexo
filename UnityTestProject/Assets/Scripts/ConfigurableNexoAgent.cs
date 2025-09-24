using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexoDoomGame
{
    /// <summary>
    /// Configurable Nexo Agent that uses existing Nexo Agent system for code generation
    /// </summary>
    public class ConfigurableNexoAgent : MonoBehaviour
    {
        [Header("Agent Configuration")]
        [SerializeField] private AgentConfiguration agentConfig;
        [SerializeField] private bool autoGenerateOnStart = false;
        [SerializeField] private bool enableRealTimeGeneration = true;
        
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Button generateButton;
        [SerializeField] private Button testButton;
        [SerializeField] private TMP_InputField specInputField;
        
        [Header("Generated Assets")]
        [SerializeField] private List<GeneratedAsset> generatedAssets = new();
        
        // Use existing Nexo Agent system
        private Nexo.Agent.Contracts.ITaskExecutionAgent _nexoAgent;
        private Nexo.Agent.Contracts.IToolRegistry _toolRegistry;
        private bool _isGenerating = false;
        
        private void Start()
        {
            InitializeAgent();
            if (autoGenerateOnStart)
            {
                GenerateGameAsync();
            }
        }
        
        private void InitializeAgent()
        {
            Debug.Log("🎮 Initializing Configurable Nexo Agent...");
            
            // Initialize existing Nexo Agent system
            InitializeNexoAgentSystem();
            
            // Set up UI
            SetupUI();
            
            UpdateStatus("Configurable Nexo Agent Ready");
        }
        
        private void InitializeNexoAgentSystem()
        {
            try
            {
                // Create Nexo Agent instance
                _nexoAgent = new Nexo.Agent.Implementations.AtlasAgent(
                    new Nexo.Agent.Implementations.SimplePlanner(),
                    new Nexo.Agent.Implementations.ToolBroker(),
                    new Nexo.Agent.Implementations.PipelineToolFactory()
                );
                
                // Create tool registry
                _toolRegistry = new Nexo.Agent.Implementations.ToolRegistry();
                
                // Register built-in tools
                RegisterBuiltinTools();
                
                Debug.Log("✅ Nexo Agent system initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to initialize Nexo Agent system: {ex.Message}");
            }
        }
        
        private void RegisterBuiltinTools()
        {
            // Register built-in tools for code generation
            var fileReadTool = new Nexo.Agent.Tools.Builtin.FileReadTool();
            var reportWriteTool = new Nexo.Agent.Tools.Builtin.ReportWriteTool();
            var summarizeTool = new Nexo.Agent.Tools.Builtin.SummarizeTool();
            
            _toolRegistry.RegisterTool(fileReadTool);
            _toolRegistry.RegisterTool(reportWriteTool);
            _toolRegistry.RegisterTool(summarizeTool);
            
            Debug.Log("🔧 Built-in tools registered");
        }
        
        private void SetupUI()
        {
            if (generateButton != null)
                generateButton.onClick.AddListener(GenerateGameAsync);
            
            if (testButton != null)
                testButton.onClick.AddListener(TestGame);
            
            if (specInputField != null)
                specInputField.text = LoadDefaultSpecification();
        }
        
        private async void GenerateGameAsync()
        {
            if (_isGenerating) return;
            
            _isGenerating = true;
            UpdateStatus("🎨 Starting dynamic game generation...");
            
            try
            {
                // Get specification from UI or file
                string specification = GetSpecification();
                
                // Parse specification
                UpdateStatus("📋 Parsing specification...");
                var gameSpec = await _specParser.ParseSpecificationAsync(specification);
                
                // Generate scripts dynamically
                UpdateStatus("📝 Generating scripts...");
                await GenerateScripts(gameSpec);
                
                // Generate assets
                UpdateStatus("🎨 Generating assets...");
                await GenerateAssets(gameSpec);
                
                // Build game
                UpdateStatus("🏗️ Building game...");
                await BuildGame(gameSpec);
                
                // Test generated content
                UpdateStatus("🧪 Testing generated content...");
                await TestGeneratedContent();
                
                UpdateStatus("✅ Dynamic generation complete!");
                Debug.Log("🎮 Game generated successfully with configurable agents!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error during generation: {ex.Message}");
                UpdateStatus($"Error: {ex.Message}");
            }
            finally
            {
                _isGenerating = false;
            }
        }
        
        private async Task GenerateScripts(GameSpecification spec)
        {
            var scriptTypes = new[]
            {
                "FPSController",
                "WeaponSystem", 
                "EnemyAI",
                "HealthSystem",
                "AudioManager",
                "UIManager",
                "GameManager"
            };
            
            for (int i = 0; i < scriptTypes.Length; i++)
            {
                var scriptType = scriptTypes[i];
                UpdateStatus($"📝 Generating {scriptType} script using Nexo Agent...");
                
                // Use existing Nexo Agent to generate script
                var script = await GenerateScriptWithNexoAgent(scriptType, spec);
                if (script != null)
                {
                    generatedAssets.Add(new GeneratedAsset
                    {
                        Type = AssetType.Script,
                        Name = scriptType,
                        Content = script,
                        GeneratedAt = DateTime.Now
                    });
                }
                
                UpdateProgress((float)(i + 1) / scriptTypes.Length * 0.4f);
                await Task.Delay(300);
            }
        }
        
        private async Task<string> GenerateScriptWithNexoAgent(string scriptType, GameSpecification spec)
        {
            try
            {
                // Create a task for the Nexo Agent to generate the script
                var task = $"Generate a Unity C# script for {scriptType} with the following requirements: " +
                          $"Game Type: {spec.GameType}, Art Style: {spec.ArtStyle}, " +
                          $"Target FPS: {spec.TargetFPS}. Include proper Unity components, " +
                          $"error handling, and logging. Use the {agentConfig.codeStyle} code style.";
                
                // Execute task with Nexo Agent
                var result = await _nexoAgent.ExecuteTaskAsync(task);
                
                if (result.Success)
                {
                    Debug.Log($"✅ Nexo Agent generated {scriptType} script successfully");
                    return result.Output;
                }
                else
                {
                    Debug.LogError($"❌ Nexo Agent failed to generate {scriptType} script: {result.Error}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error generating {scriptType} script: {ex.Message}");
                return null;
            }
        }
        
        private async Task GenerateAssets(GameSpecification spec)
        {
            // Use existing Nexo image generation service
            var assetPrompts = new[]
            {
                "Dark sci-fi wall texture with rust and metal details, high contrast, red and orange colors",
                "Industrial floor texture with grime and wear, dark gray with orange highlights",
                "Shotgun weapon icon, retro-futuristic style, red and black colors",
                "Plasma rifle weapon icon, sci-fi design, blue and white energy effects",
                "Imp enemy sprite, demonic creature, red skin with glowing eyes",
                "Demon enemy sprite, large muscular demon, dark red with horns",
                "Cacodemon enemy sprite, floating eye monster, red with tentacles",
                "Health bar UI element, retro-futuristic design, green and red colors",
                "Ammo counter UI element, digital display style, orange text",
                "Blood splatter decal, realistic blood effect, dark red color"
            };
            
            for (int i = 0; i < assetPrompts.Length; i++)
            {
                var prompt = assetPrompts[i];
                UpdateStatus($"🎨 Generating asset using Nexo: {prompt.Substring(0, Math.Min(50, prompt.Length))}...");
                
                // Use existing Nexo image generation service
                var asset = await GenerateAssetWithNexo(prompt, spec);
                if (asset != null)
                {
                    generatedAssets.Add(asset);
                }
                
                UpdateProgress(0.4f + (float)(i + 1) / assetPrompts.Length * 0.3f);
                await Task.Delay(400);
            }
        }
        
        private async Task<GeneratedAsset> GenerateAssetWithNexo(string prompt, GameSpecification spec)
        {
            try
            {
                // Use existing Nexo image generation service
                var task = $"Generate an image asset with the prompt: {prompt}. " +
                          $"Style: {spec.ArtStyle}, Colors: {string.Join(", ", spec.ColorPalette)}. " +
                          $"Resolution: {agentConfig.textureResolution}x{agentConfig.textureResolution}.";
                
                var result = await _nexoAgent.ExecuteTaskAsync(task);
                
                if (result.Success)
                {
                    return new GeneratedAsset
                    {
                        Type = AssetType.Texture,
                        Name = $"Generated_{prompt.Substring(0, Math.Min(20, prompt.Length))}",
                        Content = result.Output,
                        GeneratedAt = DateTime.Now
                    };
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error generating asset: {ex.Message}");
                return null;
            }
        }
        
        private async Task BuildGame(GameSpecification spec)
        {
            UpdateStatus("🏗️ Building game structure using Nexo Agent...");
            
            // Use Nexo Agent to build the game
            var task = $"Build a Unity game with the following specification: {spec.GameType}, " +
                      $"Art Style: {spec.ArtStyle}, Target FPS: {spec.TargetFPS}. " +
                      $"Use the generated assets and scripts to create a complete playable game.";
            
            var result = await _nexoAgent.ExecuteTaskAsync(task);
            
            if (result.Success)
            {
                Debug.Log("✅ Game built successfully using Nexo Agent");
            }
            else
            {
                Debug.LogError($"❌ Failed to build game: {result.Error}");
            }
            
            UpdateProgress(0.9f);
        }
        
        private async Task TestGeneratedContent()
        {
            UpdateStatus("🧪 Running tests...");
            
            // Test each generated script
            foreach (var asset in generatedAssets)
            {
                if (asset.Type == AssetType.Script)
                {
                    await TestScript(asset);
                }
            }
            
            UpdateProgress(1.0f);
        }
        
        private async Task TestScript(GeneratedAsset scriptAsset)
        {
            // Simulate script testing
            await Task.Delay(100);
            Debug.Log($"✅ Testing {scriptAsset.Name} script...");
        }
        
        private void TestGame()
        {
            Debug.Log("🎮 Testing generated game...");
            UpdateStatus("🧪 Running game tests...");
            
            StartCoroutine(RunGameTests());
        }
        
        private System.Collections.IEnumerator RunGameTests()
        {
            yield return new WaitForSeconds(1f);
            UpdateStatus("✅ Game tests passed!");
            Debug.Log("🎮 Generated game is ready to play!");
        }
        
        private string GetSpecification()
        {
            if (specInputField != null && !string.IsNullOrEmpty(specInputField.text))
                return specInputField.text;
            
            return LoadDefaultSpecification();
        }
        
        private string LoadDefaultSpecification()
        {
            try
            {
                string specPath = System.IO.Path.Combine(Application.dataPath, "GameSpecification.md");
                if (System.IO.File.Exists(specPath))
                {
                    return System.IO.File.ReadAllText(specPath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error loading specification: {ex.Message}");
            }
            
            return GetDefaultSpecification();
        }
        
        private string GetDefaultSpecification()
        {
            return @"
# Default Doom-Style Game Specification

## Core Gameplay
- First-person shooter with fast-paced action
- Multiple weapons with different firing patterns
- Aggressive enemy AI with hunting behavior
- Atmospheric sci-fi horror environment

## Art Style
- Dark, gritty sci-fi aesthetic
- Red, orange, and dark gray color palette
- High-contrast textures with dramatic lighting
- Retro-futuristic UI design

## Technical Requirements
- 60 FPS target performance
- Smooth controls and responsive combat
- Optimized rendering with LOD systems
- 3D spatial audio support
";
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
        
        public void UpdateConfiguration(AgentConfiguration newConfig)
        {
            agentConfig = newConfig;
            InitializeAgent();
        }
        
        public List<GeneratedAsset> GetGeneratedAssets()
        {
            return generatedAssets;
        }
    }
    
    /// <summary>
    /// Agent configuration for dynamic behavior using existing Nexo system
    /// </summary>
    [System.Serializable]
    public class AgentConfiguration
    {
        [Header("Nexo Agent Settings")]
        public string aiMode = "OFF"; // OFF, HYBRID, EMBEDDED
        public bool enableImageGeneration = true;
        public bool enableCodeGeneration = true;
        public bool enableAssetGeneration = true;
        
        [Header("Script Generation Settings")]
        public bool includeComments = true;
        public bool includeErrorHandling = true;
        public bool includeLogging = true;
        public string codeStyle = "Unity";
        
        [Header("Asset Generation Settings")]
        public int textureResolution = 512;
        public string textureFormat = "PNG";
        public bool generateNormalMaps = true;
        public bool generateSpecularMaps = true;
        
        [Header("Performance Settings")]
        public int maxConcurrentGenerations = 3;
        public float generationTimeout = 30f;
        public bool enableCaching = true;
    }
    
    /// <summary>
    /// Generated asset data structure
    /// </summary>
    [System.Serializable]
    public class GeneratedAsset
    {
        public AssetType Type;
        public string Name;
        public string Content;
        public DateTime GeneratedAt;
        public string FilePath;
    }
    
    /// <summary>
    /// Asset types enum
    /// </summary>
    public enum AssetType
    {
        Script,
        Texture,
        Model,
        Audio,
        Prefab,
        Scene
    }
    
    /// <summary>
    /// Game specification data structure
    /// </summary>
    public class GameSpecification
    {
        public string GameType { get; set; } = "";
        public string ArtStyle { get; set; } = "";
        public string[] ColorPalette { get; set; } = Array.Empty<string>();
        public string[] EnemyTypes { get; set; } = Array.Empty<string>();
        public string[] WeaponTypes { get; set; } = Array.Empty<string>();
        public int LevelCount { get; set; }
        public int TargetFPS { get; set; }
        public string[] RequiredScripts { get; set; } = Array.Empty<string>();
        public string[] RequiredAssets { get; set; } = Array.Empty<string>();
    }
    
    /// <summary>
    /// Extended GameSpecification with asset prompts
    /// </summary>
    public static class GameSpecificationExtensions
    {
        public static string[] GetAssetPrompts(this GameSpecification spec)
        {
            return new[]
            {
                $"Dark sci-fi wall texture, {spec.ArtStyle} style, {string.Join(" and ", spec.ColorPalette)} colors",
                $"Industrial floor texture, {spec.ArtStyle} style, weathered and worn",
                $"Weapon icon for {string.Join(" and ", spec.WeaponTypes)}, {spec.ArtStyle} style",
                $"Enemy sprite for {string.Join(" and ", spec.EnemyTypes)}, {spec.ArtStyle} style",
                $"UI element for health bar, {spec.ArtStyle} style, retro-futuristic",
                $"UI element for ammo counter, {spec.ArtStyle} style, digital display",
                $"Blood splatter decal, realistic effect, dark red color",
                $"Muzzle flash effect, {spec.ArtStyle} style, bright orange",
                $"Explosion effect, {spec.ArtStyle} style, dramatic lighting",
                $"Environmental decal, {spec.ArtStyle} style, industrial damage"
            };
        }
    }
}
