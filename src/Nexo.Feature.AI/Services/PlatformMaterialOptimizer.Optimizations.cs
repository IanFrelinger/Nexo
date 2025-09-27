using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Services
{
    /// <summary>
    /// Core optimization methods for PlatformMaterialOptimizer.
    /// </summary>
    public partial class PlatformMaterialOptimizer
    {
        /// <summary>
        /// Optimizes shaders for platform constraints
        /// </summary>
        private async Task<Material> OptimizeShadersAsync(Material material, PlatformConstraints constraints)
        {
            _logger.LogDebug("Optimizing shaders for platform constraints");

            var optimizedMaterial = material.Clone();
            
            // Optimize shader complexity based on platform capabilities
            if (constraints.MaxShaderComplexity < ShaderComplexity.High)
            {
                optimizedMaterial.ShaderComplexity = constraints.MaxShaderComplexity;
            }
            
            // Optimize individual shaders
            foreach (var shader in optimizedMaterial.Shaders)
            {
                var optimizedShader = await _shaderOptimizer.OptimizeShaderAsync(shader, constraints);
                optimizedMaterial.Shaders[optimizedMaterial.Shaders.IndexOf(shader)] = optimizedShader;
            }
            
            return optimizedMaterial;
        }

        /// <summary>
        /// Optimizes textures for platform constraints
        /// </summary>
        private async Task<Material> OptimizeTexturesAsync(Material material, PlatformConstraints constraints)
        {
            _logger.LogDebug("Optimizing textures for platform constraints");

            var optimizedMaterial = material.Clone();
            
            // Optimize texture resolution based on platform capabilities
            if (constraints.MaxTextureResolution < 2048)
            {
                optimizedMaterial.Textures = optimizedMaterial.Textures.Select(t => 
                    _textureOptimizer.ReduceResolution(t, constraints.MaxTextureResolution)).ToList();
            }
            
            // Optimize texture compression
            if (constraints.RequiresTextureCompression)
            {
                optimizedMaterial.Textures = optimizedMaterial.Textures.Select(t => 
                    _textureOptimizer.CompressTexture(t, constraints.CompressionFormat)).ToList();
            }
            
            return optimizedMaterial;
        }

        /// <summary>
        /// Combines platform constraints with performance requirements
        /// </summary>
        private PlatformConstraints CombineConstraints(PlatformConstraints platformConstraints, PerformanceRequirements requirements)
        {
            return new PlatformConstraints
            {
                MaxShaderComplexity = Math.Min(platformConstraints.MaxShaderComplexity, 
                    requirements.TargetFPS < 60 ? ShaderComplexity.Low : ShaderComplexity.High),
                MaxTextureResolution = Math.Min(platformConstraints.MaxTextureResolution, 
                    CalculateMaxTextureResolution(requirements.MemoryLimit)),
                RequiresTextureCompression = platformConstraints.RequiresTextureCompression || 
                    requirements.MemoryLimit < 1024 * 1024 * 1024, // 1GB
                CompressionFormat = platformConstraints.CompressionFormat,
                MaxDrawCalls = Math.Min(platformConstraints.MaxDrawCalls, requirements.MaxDrawCalls),
                MaxVertices = Math.Min(platformConstraints.MaxVertices, requirements.MaxVertices)
            };
        }

        /// <summary>
        /// Applies combined optimizations
        /// </summary>
        private async Task<Material> ApplyCombinedOptimizationsAsync(Material material, PlatformConstraints constraints)
        {
            var optimizedMaterial = material.Clone();
            
            // Apply shader optimizations
            optimizedMaterial = await OptimizeShadersAsync(optimizedMaterial, constraints);
            
            // Apply texture optimizations
            optimizedMaterial = await OptimizeTexturesAsync(optimizedMaterial, constraints);
            
            // Apply additional optimizations based on constraints
            if (constraints.MaxDrawCalls < 100)
            {
                optimizedMaterial = await OptimizeForLowDrawCallsAsync(optimizedMaterial);
            }
            
            if (constraints.MaxVertices < 10000)
            {
                optimizedMaterial = await OptimizeForLowVerticesAsync(optimizedMaterial);
            }
            
            return optimizedMaterial;
        }

        /// <summary>
        /// Validates optimization results
        /// </summary>
        private async Task ValidateOptimizationAsync(Material material, PlatformConstraints constraints)
        {
            _logger.LogDebug("Validating material optimization");

            // Validate shader complexity
            if (material.ShaderComplexity > constraints.MaxShaderComplexity)
            {
                throw new InvalidOperationException($"Shader complexity {material.ShaderComplexity} exceeds platform limit {constraints.MaxShaderComplexity}");
            }
            
            // Validate texture resolution
            foreach (var texture in material.Textures)
            {
                if (texture.Width > constraints.MaxTextureResolution || texture.Height > constraints.MaxTextureResolution)
                {
                    throw new InvalidOperationException($"Texture resolution {texture.Width}x{texture.Height} exceeds platform limit {constraints.MaxTextureResolution}");
                }
            }
            
            // Validate draw calls
            if (material.EstimatedDrawCalls > constraints.MaxDrawCalls)
            {
                throw new InvalidOperationException($"Estimated draw calls {material.EstimatedDrawCalls} exceeds platform limit {constraints.MaxDrawCalls}");
            }
        }

        /// <summary>
        /// Calculates maximum texture resolution based on memory limit
        /// </summary>
        private int CalculateMaxTextureResolution(long memoryLimit)
        {
            // Calculate maximum texture resolution based on memory limit
            var availableMemory = memoryLimit * 0.8f; // Use 80% of available memory
            var textureCount = 4; // Assume 4 textures per material
            var bytesPerPixel = 4; // RGBA
            
            var maxResolution = (int)Math.Sqrt(availableMemory / (textureCount * bytesPerPixel));
            
            // Round down to nearest power of 2
            var power = (int)Math.Floor(Math.Log2(maxResolution));
            return (int)Math.Pow(2, power);
        }
    }
}
