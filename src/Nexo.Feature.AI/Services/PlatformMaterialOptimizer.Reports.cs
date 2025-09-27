using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Services
{
    /// <summary>
    /// Report generation for PlatformMaterialOptimizer.
    /// </summary>
    public partial class PlatformMaterialOptimizer
    {
        /// <summary>
        /// Analyzes optimizations applied
        /// </summary>
        private async Task AnalyzeOptimizationsAppliedAsync(OptimizationReport report)
        {
            var original = report.OriginalMaterial;
            var optimized = report.OptimizedMaterial;
            
            // Analyze shader optimizations
            if (original.ShaderComplexity != optimized.ShaderComplexity)
            {
                report.OptimizationsApplied.Add(new OptimizationDetail
                {
                    Type = OptimizationType.ShaderComplexity,
                    Description = $"Reduced shader complexity from {original.ShaderComplexity} to {optimized.ShaderComplexity}",
                    Impact = OptimizationImpact.Medium
                });
            }
            
            // Analyze texture optimizations
            if (original.Textures.Count != optimized.Textures.Count)
            {
                report.OptimizationsApplied.Add(new OptimizationDetail
                {
                    Type = OptimizationType.TextureCount,
                    Description = $"Reduced texture count from {original.Textures.Count} to {optimized.Textures.Count}",
                    Impact = OptimizationImpact.High
                });
            }
            
            // Analyze material property optimizations
            if (original.BaseProperties.Metallic != optimized.BaseProperties.Metallic)
            {
                report.OptimizationsApplied.Add(new OptimizationDetail
                {
                    Type = OptimizationType.MaterialProperties,
                    Description = $"Adjusted metallic property from {original.BaseProperties.Metallic} to {optimized.BaseProperties.Metallic}",
                    Impact = OptimizationImpact.Low
                });
            }
        }

        /// <summary>
        /// Calculates performance improvements
        /// </summary>
        private async Task CalculatePerformanceImprovementsAsync(OptimizationReport report)
        {
            var original = report.OriginalMaterial;
            var optimized = report.OptimizedMaterial;
            
            // Calculate memory usage improvement
            var originalMemory = CalculateMemoryUsage(original);
            var optimizedMemory = CalculateMemoryUsage(optimized);
            var memoryImprovement = (originalMemory - optimizedMemory) / originalMemory * 100;
            
            report.PerformanceImprovements.MemoryUsageImprovement = memoryImprovement;
            
            // Calculate draw call improvement
            var originalDrawCalls = original.EstimatedDrawCalls;
            var optimizedDrawCalls = optimized.EstimatedDrawCalls;
            var drawCallImprovement = (originalDrawCalls - optimizedDrawCalls) / (double)originalDrawCalls * 100;
            
            report.PerformanceImprovements.DrawCallImprovement = drawCallImprovement;
            
            // Calculate shader complexity improvement
            var originalComplexity = (int)original.ShaderComplexity;
            var optimizedComplexity = (int)optimized.ShaderComplexity;
            var complexityImprovement = (originalComplexity - optimizedComplexity) / (double)originalComplexity * 100;
            
            report.PerformanceImprovements.ShaderComplexityImprovement = complexityImprovement;
        }

        /// <summary>
        /// Generates optimization recommendations
        /// </summary>
        private async Task GenerateRecommendationsAsync(OptimizationReport report)
        {
            var recommendations = new List<OptimizationRecommendation>();
            
            // Generate recommendations based on optimization results
            if (report.PerformanceImprovements.MemoryUsageImprovement < 20)
            {
                recommendations.Add(new OptimizationRecommendation
                {
                    Type = RecommendationType.MemoryOptimization,
                    Priority = RecommendationPriority.High,
                    Description = "Consider further reducing texture resolution or using texture compression",
                    Action = "Reduce texture resolution or enable compression"
                });
            }
            
            if (report.PerformanceImprovements.DrawCallImprovement < 10)
            {
                recommendations.Add(new OptimizationRecommendation
                {
                    Type = RecommendationType.DrawCallOptimization,
                    Priority = RecommendationPriority.Medium,
                    Description = "Consider using texture atlasing to reduce draw calls",
                    Action = "Implement texture atlasing"
                });
            }
            
            if (report.PerformanceImprovements.ShaderComplexityImprovement < 30)
            {
                recommendations.Add(new OptimizationRecommendation
                {
                    Type = RecommendationType.ShaderOptimization,
                    Priority = RecommendationPriority.Medium,
                    Description = "Consider simplifying shaders further for better performance",
                    Action = "Simplify shader complexity"
                });
            }
            
            report.Recommendations = recommendations;
        }

        /// <summary>
        /// Calculates memory usage for a material
        /// </summary>
        private long CalculateMemoryUsage(Material material)
        {
            var memoryUsage = 0L;
            
            // Calculate texture memory usage
            foreach (var texture in material.Textures)
            {
                memoryUsage += texture.Width * texture.Height * 4; // 4 bytes per pixel (RGBA)
            }
            
            // Calculate shader memory usage
            memoryUsage += material.Shaders.Count * 1024; // 1KB per shader
            
            // Calculate material property memory usage
            memoryUsage += 1024; // 1KB for material properties
            
            return memoryUsage;
        }
    }
}
