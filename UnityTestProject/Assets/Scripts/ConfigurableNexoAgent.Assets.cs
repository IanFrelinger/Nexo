using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace NexoDoomGame
{
    /// <summary>
    /// Asset generation functionality
    /// </summary>
    public partial class ConfigurableNexoAgent
    {
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
    }
}
