using System;
using System.Threading.Tasks;
using UnityEngine;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Services;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Interfaces;

namespace NexoDoomGame
{
    /// <summary>
    /// Dynamic material generation using AI agents
    /// </summary>
    public partial class NexoGameAgent : MonoBehaviour
    {
        private async Task<Material> CreateWeaponMaterialAsync()
        {
            // Use agent-driven material generation instead of hardcoded values
            var materialRequest = new MaterialGenerationRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                Description = "Weapon material for game object - metallic, dark gray, worn surface",
                VisualStyle = "Realistic",
                TargetPlatform = PlatformType.Desktop,
                MaterialType = MaterialType.PBR
            };

            var materialGenerator = new DynamicMaterialGenerator(
                new NullLogger<DynamicMaterialGenerator>(),
                new MaterialContextAnalyzer(
                    new NullLogger<MaterialContextAnalyzer>(),
                    new ColorPaletteAnalyzer(),
                    new SurfaceTypeDetector(),
                    new PerformanceAnalyzer()
                ),
                new MaterialGenerationAgent(
                    new NullLogger<MaterialGenerationAgent>(),
                    new MaterialContextAnalyzer(
                        new NullLogger<MaterialContextAnalyzer>(),
                        new ColorPaletteAnalyzer(),
                        new SurfaceTypeDetector(),
                        new PerformanceAnalyzer()
                    ),
                    new MaterialOptimizer(),
                    new PlatformMaterialOptimizer(
                        new NullLogger<PlatformMaterialOptimizer>(),
                        new PerformanceAnalyzer(),
                        new ShaderOptimizer(),
                        new TextureOptimizer()
                    )
                ),
                new PlatformMaterialOptimizer(
                    new NullLogger<PlatformMaterialOptimizer>(),
                    new PerformanceAnalyzer(),
                    new ShaderOptimizer(),
                    new TextureOptimizer()
                ),
                new UserGuidedAgentExpansion(
                    new NullLogger<UserGuidedAgentExpansion>(),
                    new AgentCapabilityRegistry(),
                    new AgentLearningSystem(),
                    new UserFeedbackProcessor(),
                    new ExpansionStrategySelector()
                )
            );

            var result = await materialGenerator.GenerateMaterialAsync(materialRequest);
            return result.Success ? result.Material : CreateFallbackWeaponMaterial();
        }
        
        private async Task<Material> CreateEnemyMaterialAsync()
        {
            // Use agent-driven material generation instead of hardcoded values
            var materialRequest = new MaterialGenerationRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                Description = "Enemy material for game object - red, organic, slightly rough surface",
                VisualStyle = "Realistic",
                TargetPlatform = PlatformType.Desktop,
                MaterialType = MaterialType.PBR
            };

            var materialGenerator = new DynamicMaterialGenerator(
                new NullLogger<DynamicMaterialGenerator>(),
                new MaterialContextAnalyzer(
                    new NullLogger<MaterialContextAnalyzer>(),
                    new ColorPaletteAnalyzer(),
                    new SurfaceTypeDetector(),
                    new PerformanceAnalyzer()
                ),
                new MaterialGenerationAgent(
                    new NullLogger<MaterialGenerationAgent>(),
                    new MaterialContextAnalyzer(
                        new NullLogger<MaterialContextAnalyzer>(),
                        new ColorPaletteAnalyzer(),
                        new SurfaceTypeDetector(),
                        new PerformanceAnalyzer()
                    ),
                    new MaterialOptimizer(),
                    new PlatformMaterialOptimizer(
                        new NullLogger<PlatformMaterialOptimizer>(),
                        new PerformanceAnalyzer(),
                        new ShaderOptimizer(),
                        new TextureOptimizer()
                    )
                ),
                new PlatformMaterialOptimizer(
                    new NullLogger<PlatformMaterialOptimizer>(),
                    new PerformanceAnalyzer(),
                    new ShaderOptimizer(),
                    new TextureOptimizer()
                ),
                new UserGuidedAgentExpansion(
                    new NullLogger<UserGuidedAgentExpansion>(),
                    new AgentCapabilityRegistry(),
                    new AgentLearningSystem(),
                    new UserFeedbackProcessor(),
                    new ExpansionStrategySelector()
                )
            );

            var result = await materialGenerator.GenerateMaterialAsync(materialRequest);
            return result.Success ? result.Material : CreateFallbackEnemyMaterial();
        }

        private Material CreateFallbackWeaponMaterial()
        {
            // Fallback to hardcoded material if agent generation fails
            var material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            material.metallic = 0.8f;
            material.smoothness = 0.6f;
            return material;
        }
        
        private Material CreateFallbackEnemyMaterial()
        {
            // Fallback to hardcoded material if agent generation fails
            var material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            material.metallic = 0.2f;
            material.smoothness = 0.3f;
            return material;
        }
    }
}
