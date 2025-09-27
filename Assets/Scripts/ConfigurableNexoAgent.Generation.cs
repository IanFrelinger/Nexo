using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace NexoDoomGame
{
    /// <summary>
    /// Game generation functionality
    /// </summary>
    public partial class ConfigurableNexoAgent
    {
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
    }
}
