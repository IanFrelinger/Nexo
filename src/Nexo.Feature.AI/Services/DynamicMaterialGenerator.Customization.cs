using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Services
{
    /// <summary>
    /// User customization and preference application functionality
    /// </summary>
    public partial class DynamicMaterialGenerator : IDynamicMaterialGenerator
    {
        private async Task<Material> ApplyUserCustomizationsAsync(Material material, UserPreferences preferences)
        {
            if (preferences == null) return material;

            var customizedMaterial = material.Clone();

            // Apply color preferences
            if (preferences.ColorPreferences != null)
            {
                customizedMaterial = await ApplyColorPreferencesAsync(customizedMaterial, preferences.ColorPreferences);
            }

            // Apply style preferences
            if (preferences.StylePreferences != null)
            {
                customizedMaterial = await ApplyStylePreferencesAsync(customizedMaterial, preferences.StylePreferences);
            }

            // Apply performance preferences
            if (preferences.PerformancePreferences != null)
            {
                customizedMaterial = await ApplyPerformancePreferencesAsync(customizedMaterial, preferences.PerformancePreferences);
            }

            return customizedMaterial;
        }

        private async Task<Material> ApplyColorPreferencesAsync(Material material, ColorPreferences preferences)
        {
            if (preferences.PreferredColors != null && preferences.PreferredColors.Any())
            {
                material.BaseProperties.Albedo = preferences.PreferredColors.First();
            }

            if (preferences.ColorTemperature != null)
            {
                material.BaseProperties.Albedo = AdjustColorTemperature(material.BaseProperties.Albedo, preferences.ColorTemperature.Value);
            }

            return material;
        }

        private Color AdjustColorTemperature(Color color, float temperature)
        {
            // Simple color temperature adjustment
            var factor = temperature / 100f;
            return new Color(
                Math.Min(1f, color.r * factor),
                Math.Min(1f, color.g * factor),
                Math.Min(1f, color.b * factor),
                color.a
            );
        }

        private async Task<Material> ApplyStylePreferencesAsync(Material material, StylePreferences preferences)
        {
            if (preferences.StyleType == StyleType.Realistic)
            {
                material.BaseProperties.Metallic = Math.Max(0.5f, material.BaseProperties.Metallic);
                material.BaseProperties.Smoothness = Math.Max(0.7f, material.BaseProperties.Smoothness);
            }
            else if (preferences.StyleType == StyleType.Stylized)
            {
                material.BaseProperties.Metallic = Math.Min(0.3f, material.BaseProperties.Metallic);
                material.BaseProperties.Smoothness = Math.Min(0.5f, material.BaseProperties.Smoothness);
            }

            return material;
        }

        private async Task<Material> ApplyPerformancePreferencesAsync(Material material, PerformancePreferences preferences)
        {
            if (preferences.TargetFPS < 60)
            {
                // Reduce shader complexity for lower FPS targets
                material.ShaderComplexity = ShaderComplexity.Low;
            }

            if (preferences.MemoryLimit < 512 * 1024 * 1024) // 512MB
            {
                // Reduce texture resolution and complexity
                material.Textures = material.Textures.Select(t => t.ReduceQuality()).ToList();
            }

            return material;
        }
    }
}
