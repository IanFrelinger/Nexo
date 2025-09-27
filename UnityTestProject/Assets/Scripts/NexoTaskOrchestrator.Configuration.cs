using System;
using UnityEngine;

namespace NexoDoomGame
{
    /// <summary>
    /// Configuration loading and management functionality
    /// </summary>
    public partial class NexoTaskOrchestrator
    {
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
}
