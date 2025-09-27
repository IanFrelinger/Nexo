using System;
using System.Threading.Tasks;
using UnityEngine;

namespace NexoDoomGame
{
    /// <summary>
    /// Task execution and generation logic functionality
    /// </summary>
    public partial class NexoTaskOrchestrator
    {
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
    }
}
