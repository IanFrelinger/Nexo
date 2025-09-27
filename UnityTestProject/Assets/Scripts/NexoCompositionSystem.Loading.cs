using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NexoDoomGame
{
    /// <summary>
    /// Asset and script loading functionality
    /// </summary>
    public partial class NexoCompositionSystem
    {
        private IEnumerator LoadGeneratedScripts()
        {
            Debug.Log("📝 Loading generated scripts...");
            UpdateCompositionStatus("Loading Generated Scripts");
            
            try
            {
                var scriptsPath = Path.Combine(Application.dataPath, "..", generatedScriptsPath);
                
                if (Directory.Exists(scriptsPath))
                {
                    var scriptFiles = Directory.GetFiles(scriptsPath, "*.cs");
                    
                    foreach (var scriptFile in scriptFiles)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(scriptFile);
                        Debug.Log($"📝 Found generated script: {fileName}");
                        
                        _compositionResults.Add(new CompositionResult
                        {
                            Component = fileName,
                            Type = CompositionType.Script,
                            Status = CompositionStatus.Loaded,
                            Timestamp = DateTime.Now
                        });
                    }
                    
                    Debug.Log($"✅ Loaded {scriptFiles.Length} generated scripts");
                }
                else
                {
                    Debug.LogWarning($"⚠️ Generated scripts path not found: {scriptsPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to load generated scripts: {ex.Message}");
            }
            
            yield return new WaitForSeconds(1f);
        }

        private IEnumerator LoadGeneratedAssets()
        {
            Debug.Log("🎨 Loading generated assets...");
            UpdateCompositionStatus("Loading Generated Assets");
            
            try
            {
                var assetsPath = Path.Combine(Application.dataPath, "..", generatedAssetsPath);
                
                if (Directory.Exists(assetsPath))
                {
                    // Load textures
                    var texturesPath = Path.Combine(assetsPath, "Textures");
                    if (Directory.Exists(texturesPath))
                    {
                        var textureFiles = Directory.GetFiles(texturesPath, "*.png");
                        foreach (var textureFile in textureFiles)
                        {
                            var fileName = Path.GetFileNameWithoutExtension(textureFile);
                            Debug.Log($"🎨 Found generated texture: {fileName}");
                            
                            _compositionResults.Add(new CompositionResult
                            {
                                Component = fileName,
                                Type = CompositionType.Texture,
                                Status = CompositionStatus.Loaded,
                                Timestamp = DateTime.Now
                            });
                        }
                    }
                    
                    // Load models
                    var modelsPath = Path.Combine(assetsPath, "Models");
                    if (Directory.Exists(modelsPath))
                    {
                        var modelFiles = Directory.GetFiles(modelsPath, "*.fbx");
                        foreach (var modelFile in modelFiles)
                        {
                            var fileName = Path.GetFileNameWithoutExtension(modelFile);
                            Debug.Log($"🏗️ Found generated model: {fileName}");
                            
                            _compositionResults.Add(new CompositionResult
                            {
                                Component = fileName,
                                Type = CompositionType.Model,
                                Status = CompositionStatus.Loaded,
                                Timestamp = DateTime.Now
                            });
                        }
                    }
                    
                    // Load audio
                    var audioPath = Path.Combine(assetsPath, "Audio");
                    if (Directory.Exists(audioPath))
                    {
                        var audioFiles = Directory.GetFiles(audioPath, "*.wav");
                        foreach (var audioFile in audioFiles)
                        {
                            var fileName = Path.GetFileNameWithoutExtension(audioFile);
                            Debug.Log($"🔊 Found generated audio: {fileName}");
                            
                            _compositionResults.Add(new CompositionResult
                            {
                                Component = fileName,
                                Type = CompositionType.Audio,
                                Status = CompositionStatus.Loaded,
                                Timestamp = DateTime.Now
                            });
                        }
                    }
                    
                    Debug.Log("✅ Generated assets loaded");
                }
                else
                {
                    Debug.LogWarning($"⚠️ Generated assets path not found: {assetsPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to load generated assets: {ex.Message}");
            }
            
            yield return new WaitForSeconds(1f);
        }
    }
}
