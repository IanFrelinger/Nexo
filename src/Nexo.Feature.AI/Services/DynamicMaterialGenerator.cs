using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Services
{
    /// <summary>
    /// Dynamic material generation pipeline that adapts to user requirements
    /// </summary>
    public class DynamicMaterialGenerator : IDynamicMaterialGenerator
    {
        private readonly ILogger<DynamicMaterialGenerator> _logger;
        private readonly IMaterialContextAnalyzer _contextAnalyzer;
        private readonly IMaterialGenerationAgent _materialAgent;
        private readonly IPlatformOptimizer _platformOptimizer;
        private readonly IUserGuidedAgentExpansion _expansionService;

        public DynamicMaterialGenerator(
            ILogger<DynamicMaterialGenerator> logger,
            IMaterialContextAnalyzer contextAnalyzer,
            IMaterialGenerationAgent materialAgent,
            IPlatformOptimizer platformOptimizer,
            IUserGuidedAgentExpansion expansionService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _contextAnalyzer = contextAnalyzer ?? throw new ArgumentNullException(nameof(contextAnalyzer));
            _materialAgent = materialAgent ?? throw new ArgumentNullException(nameof(materialAgent));
            _platformOptimizer = platformOptimizer ?? throw new ArgumentNullException(nameof(platformOptimizer));
            _expansionService = expansionService ?? throw new ArgumentNullException(nameof(expansionService));
        }

        public async Task<MaterialGenerationResult> GenerateMaterialAsync(MaterialGenerationRequest request)
        {
            _logger.LogInformation("Generating material with dynamic pipeline: {RequestId}", request.RequestId);

            try
            {
                // Analyze the material request context
                var context = await _contextAnalyzer.AnalyzeAsync(request);
                
                // Check if we need to expand agent capabilities
                var capabilityAssessment = await AssessCapabilityRequirementsAsync(context);
                
                if (capabilityAssessment.RequiresExpansion)
                {
                    await ExpandAgentCapabilitiesAsync(capabilityAssessment);
                }

                // Generate material using the enhanced agent
                var material = await _materialAgent.GenerateMaterialAsync(context);
                
                // Optimize for target platform
                var optimizedMaterial = await _platformOptimizer.OptimizeMaterialAsync(material, context.TargetPlatform);
                
                // Apply user-specific customizations
                var customizedMaterial = await ApplyUserCustomizationsAsync(optimizedMaterial, request.UserPreferences);
                
                return new MaterialGenerationResult
                {
                    RequestId = request.RequestId,
                    Success = true,
                    Material = customizedMaterial,
                    GenerationMetadata = new MaterialGenerationMetadata
                    {
                        Context = context,
                        CapabilityAssessment = capabilityAssessment,
                        OptimizationApplied = true,
                        CustomizationsApplied = request.UserPreferences?.Any() == true
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating material: {RequestId}", request.RequestId);
                return new MaterialGenerationResult
                {
                    RequestId = request.RequestId,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<MaterialGenerationResult> GenerateMaterialBatchAsync(List<MaterialGenerationRequest> requests)
        {
            _logger.LogInformation("Generating material batch: {RequestCount} materials", requests.Count);

            var results = new List<MaterialGenerationResult>();
            var batchContext = new BatchGenerationContext
            {
                BatchId = Guid.NewGuid().ToString(),
                RequestCount = requests.Count,
                StartTime = DateTime.UtcNow
            };

            try
            {
                // Analyze all requests to identify common patterns
                var commonPatterns = await AnalyzeCommonPatternsAsync(requests);
                
                // Generate materials in parallel where possible
                var tasks = requests.Select(async request =>
                {
                    try
                    {
                        return await GenerateMaterialAsync(request);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error generating material in batch: {RequestId}", request.RequestId);
                        return new MaterialGenerationResult
                        {
                            RequestId = request.RequestId,
                            Success = false,
                            ErrorMessage = ex.Message
                        };
                    }
                });

                results = (await Task.WhenAll(tasks)).ToList();
                
                batchContext.EndTime = DateTime.UtcNow;
                batchContext.Duration = batchContext.EndTime - batchContext.StartTime;
                batchContext.SuccessCount = results.Count(r => r.Success);
                batchContext.FailureCount = results.Count(r => !r.Success);

                _logger.LogInformation("Batch generation complete: {SuccessCount}/{TotalCount} successful", 
                    batchContext.SuccessCount, batchContext.RequestCount);

                return new MaterialGenerationResult
                {
                    RequestId = batchContext.BatchId,
                    Success = true,
                    BatchResults = results,
                    GenerationMetadata = new MaterialGenerationMetadata
                    {
                        BatchContext = batchContext,
                        CommonPatterns = commonPatterns
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch material generation");
                throw;
            }
        }

        public async Task<MaterialGenerationResult> RefineMaterialAsync(MaterialRefinementRequest request)
        {
            _logger.LogInformation("Refining material: {RequestId}", request.RequestId);

            try
            {
                // Analyze the refinement requirements
                var refinementContext = await AnalyzeRefinementRequirementsAsync(request);
                
                // Apply refinements
                var refinedMaterial = await ApplyRefinementsAsync(request.OriginalMaterial, refinementContext);
                
                // Re-optimize if necessary
                if (refinementContext.RequiresReoptimization)
                {
                    refinedMaterial = await _platformOptimizer.OptimizeMaterialAsync(refinedMaterial, request.TargetPlatform);
                }

                return new MaterialGenerationResult
                {
                    RequestId = request.RequestId,
                    Success = true,
                    Material = refinedMaterial,
                    GenerationMetadata = new MaterialGenerationMetadata
                    {
                        RefinementContext = refinementContext,
                        ReoptimizationApplied = refinementContext.RequiresReoptimization
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refining material: {RequestId}", request.RequestId);
                return new MaterialGenerationResult
                {
                    RequestId = request.RequestId,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<MaterialGenerationResult> GenerateMaterialVariationsAsync(MaterialVariationRequest request)
        {
            _logger.LogInformation("Generating material variations: {RequestId}", request.RequestId);

            try
            {
                var variations = new List<Material>();
                var variationContext = await AnalyzeVariationRequirementsAsync(request);
                
                // Generate base material
                var baseMaterial = await GenerateMaterialAsync(request.BaseRequest);
                
                if (!baseMaterial.Success)
                {
                    return new MaterialGenerationResult
                    {
                        RequestId = request.RequestId,
                        Success = false,
                        ErrorMessage = "Failed to generate base material for variations"
                    };
                }

                variations.Add(baseMaterial.Material);

                // Generate variations
                foreach (var variation in variationContext.Variations)
                {
                    var variationMaterial = await GenerateVariationAsync(baseMaterial.Material, variation);
                    variations.Add(variationMaterial);
                }

                return new MaterialGenerationResult
                {
                    RequestId = request.RequestId,
                    Success = true,
                    MaterialVariations = variations,
                    GenerationMetadata = new MaterialGenerationMetadata
                    {
                        VariationContext = variationContext,
                        VariationCount = variations.Count
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating material variations: {RequestId}", request.RequestId);
                return new MaterialGenerationResult
                {
                    RequestId = request.RequestId,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<CapabilityAssessment> AssessCapabilityRequirementsAsync(MaterialContext context)
        {
            var assessment = new CapabilityAssessment
            {
                IsFeasible = true,
                EstimatedComplexity = ExpansionComplexity.Low,
                RequiredResources = new ResourceRequirements(),
                PotentialConflicts = new List<PotentialConflict>(),
                RecommendedApproach = ExpansionApproach.Incremental
            };

            // Check if we have the required capabilities for this material type
            if (context.MaterialType == MaterialType.PBR && !await HasPBRCapabilityAsync())
            {
                assessment.RequiresExpansion = true;
                assessment.RequiredCapabilities.Add(CapabilityType.MaterialGeneration);
            }

            // Check for advanced features that might require expansion
            if (context.SurfaceDetails.Count > 3 && !await HasAdvancedSurfaceCapabilityAsync())
            {
                assessment.RequiresExpansion = true;
                assessment.RequiredCapabilities.Add(CapabilityType.TextureGeneration);
            }

            if (context.LightingRequirements.Any(r => r.Type == LightingType.Volumetric) && !await HasVolumetricLightingCapabilityAsync())
            {
                assessment.RequiresExpansion = true;
                assessment.RequiredCapabilities.Add(CapabilityType.ShaderGeneration);
            }

            return assessment;
        }

        private async Task ExpandAgentCapabilitiesAsync(CapabilityAssessment assessment)
        {
            if (!assessment.RequiresExpansion) return;

            var expansionRequest = new AgentExpansionRequest
            {
                AgentId = _materialAgent.AgentId,
                DesiredCapabilities = assessment.RequiredCapabilities.Select(c => new DesiredCapability
                {
                    Type = c,
                    Level = CapabilityLevel.Intermediate,
                    Priority = CapabilityPriority.Medium
                }).ToList(),
                Constraints = new ExpansionConstraints
                {
                    MaxMemoryUsage = 1024 * 1024 * 1024, // 1GB
                    MaxProcessingPower = ProcessingPower.High,
                    MaxStorageSpace = 100 * 1024 * 1024, // 100MB
                    TimeLimit = TimeSpan.FromMinutes(30)
                }
            };

            await _expansionService.ExpandAgentCapabilitiesAsync(expansionRequest);
        }

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

        private async Task<List<CommonPattern>> AnalyzeCommonPatternsAsync(List<MaterialGenerationRequest> requests)
        {
            var patterns = new List<CommonPattern>();

            // Analyze material type patterns
            var materialTypeGroups = requests.GroupBy(r => r.MaterialType);
            foreach (var group in materialTypeGroups)
            {
                patterns.Add(new CommonPattern
                {
                    Type = PatternType.MaterialType,
                    Value = group.Key.ToString(),
                    Frequency = group.Count(),
                    Confidence = (double)group.Count() / requests.Count
                });
            }

            // Analyze visual style patterns
            var styleGroups = requests.GroupBy(r => r.VisualStyle);
            foreach (var group in styleGroups)
            {
                patterns.Add(new CommonPattern
                {
                    Type = PatternType.VisualStyle,
                    Value = group.Key,
                    Frequency = group.Count(),
                    Confidence = (double)group.Count() / requests.Count
                });
            }

            return patterns;
        }

        private async Task<RefinementContext> AnalyzeRefinementRequirementsAsync(MaterialRefinementRequest request)
        {
            return new RefinementContext
            {
                RefinementType = request.RefinementType,
                Requirements = request.Requirements,
                RequiresReoptimization = request.RefinementType == RefinementType.PerformanceOptimization
            };
        }

        private async Task<Material> ApplyRefinementsAsync(Material originalMaterial, RefinementContext context)
        {
            var refinedMaterial = originalMaterial.Clone();

            switch (context.RefinementType)
            {
                case RefinementType.ColorAdjustment:
                    refinedMaterial = await ApplyColorAdjustmentsAsync(refinedMaterial, context.Requirements);
                    break;
                case RefinementType.SurfaceModification:
                    refinedMaterial = await ApplySurfaceModificationsAsync(refinedMaterial, context.Requirements);
                    break;
                case RefinementType.PerformanceOptimization:
                    refinedMaterial = await ApplyPerformanceOptimizationsAsync(refinedMaterial, context.Requirements);
                    break;
            }

            return refinedMaterial;
        }

        private async Task<Material> ApplyColorAdjustmentsAsync(Material material, List<RefinementRequirement> requirements)
        {
            foreach (var requirement in requirements)
            {
                if (requirement.Type == RefinementRequirementType.ColorAdjustment)
                {
                    material.BaseProperties.Albedo = requirement.ColorValue;
                }
            }
            return material;
        }

        private async Task<Material> ApplySurfaceModificationsAsync(Material material, List<RefinementRequirement> requirements)
        {
            foreach (var requirement in requirements)
            {
                if (requirement.Type == RefinementRequirementType.SurfaceModification)
                {
                    material.BaseProperties.Metallic = requirement.FloatValue;
                    material.BaseProperties.Smoothness = requirement.FloatValue2;
                }
            }
            return material;
        }

        private async Task<Material> ApplyPerformanceOptimizationsAsync(Material material, List<RefinementRequirement> requirements)
        {
            foreach (var requirement in requirements)
            {
                if (requirement.Type == RefinementRequirementType.PerformanceOptimization)
                {
                    material.ShaderComplexity = ShaderComplexity.Low;
                    material.Textures = material.Textures.Select(t => t.ReduceQuality()).ToList();
                }
            }
            return material;
        }

        private async Task<VariationContext> AnalyzeVariationRequirementsAsync(MaterialVariationRequest request)
        {
            return new VariationContext
            {
                VariationCount = request.VariationCount,
                VariationType = request.VariationType,
                Variations = GenerateVariationSpecifications(request)
            };
        }

        private List<VariationSpecification> GenerateVariationSpecifications(MaterialVariationRequest request)
        {
            var variations = new List<VariationSpecification>();

            for (int i = 0; i < request.VariationCount; i++)
            {
                variations.Add(new VariationSpecification
                {
                    VariationId = Guid.NewGuid().ToString(),
                    Type = request.VariationType,
                    Parameters = GenerateVariationParameters(request.VariationType, i)
                });
            }

            return variations;
        }

        private Dictionary<string, object> GenerateVariationParameters(VariationType variationType, int index)
        {
            return variationType switch
            {
                VariationType.ColorVariation => new Dictionary<string, object>
                {
                    ["Hue"] = (index * 30) % 360,
                    ["Saturation"] = 0.8f + (index * 0.1f),
                    ["Brightness"] = 0.5f + (index * 0.1f)
                },
                VariationType.SurfaceVariation => new Dictionary<string, object>
                {
                    ["Metallic"] = 0.2f + (index * 0.2f),
                    ["Smoothness"] = 0.3f + (index * 0.2f),
                    ["Roughness"] = 0.1f + (index * 0.1f)
                },
                _ => new Dictionary<string, object>()
            };
        }

        private async Task<Material> GenerateVariationAsync(Material baseMaterial, VariationSpecification variation)
        {
            var variationMaterial = baseMaterial.Clone();

            switch (variation.Type)
            {
                case VariationType.ColorVariation:
                    variationMaterial = await ApplyColorVariationAsync(variationMaterial, variation.Parameters);
                    break;
                case VariationType.SurfaceVariation:
                    variationMaterial = await ApplySurfaceVariationAsync(variationMaterial, variation.Parameters);
                    break;
            }

            return variationMaterial;
        }

        private async Task<Material> ApplyColorVariationAsync(Material material, Dictionary<string, object> parameters)
        {
            if (parameters.ContainsKey("Hue"))
            {
                var hue = (float)parameters["Hue"];
                material.BaseProperties.Albedo = AdjustHue(material.BaseProperties.Albedo, hue);
            }
            return material;
        }

        private async Task<Material> ApplySurfaceVariationAsync(Material material, Dictionary<string, object> parameters)
        {
            if (parameters.ContainsKey("Metallic"))
            {
                material.BaseProperties.Metallic = (float)parameters["Metallic"];
            }
            if (parameters.ContainsKey("Smoothness"))
            {
                material.BaseProperties.Smoothness = (float)parameters["Smoothness"];
            }
            return material;
        }

        private Color AdjustHue(Color color, float hue)
        {
            // Simple hue adjustment
            var hsv = ColorToHSV(color);
            hsv.H = hue;
            return HSVToColor(hsv);
        }

        private HSVColor ColorToHSV(Color color)
        {
            // Convert RGB to HSV
            var max = Math.Max(color.r, Math.Max(color.g, color.b));
            var min = Math.Min(color.r, Math.Min(color.g, color.b));
            var delta = max - min;

            var h = 0f;
            if (delta != 0)
            {
                if (max == color.r)
                    h = ((color.g - color.b) / delta) % 6;
                else if (max == color.g)
                    h = (color.b - color.r) / delta + 2;
                else
                    h = (color.r - color.g) / delta + 4;
            }

            h *= 60;
            if (h < 0) h += 360;

            return new HSVColor
            {
                H = h,
                S = max == 0 ? 0 : delta / max,
                V = max
            };
        }

        private Color HSVToColor(HSVColor hsv)
        {
            // Convert HSV to RGB
            var c = hsv.V * hsv.S;
            var x = c * (1 - Math.Abs((hsv.H / 60) % 2 - 1));
            var m = hsv.V - c;

            float r, g, b;
            if (hsv.H < 60)
            {
                r = c; g = x; b = 0;
            }
            else if (hsv.H < 120)
            {
                r = x; g = c; b = 0;
            }
            else if (hsv.H < 180)
            {
                r = 0; g = c; b = x;
            }
            else if (hsv.H < 240)
            {
                r = 0; g = x; b = c;
            }
            else if (hsv.H < 300)
            {
                r = x; g = 0; b = c;
            }
            else
            {
                r = c; g = 0; b = x;
            }

            return new Color(r + m, g + m, b + m, 1f);
        }

        private async Task<bool> HasPBRCapabilityAsync()
        {
            // Check if the agent has PBR material generation capability
            return await _materialAgent.HasCapabilityAsync(CapabilityType.MaterialGeneration);
        }

        private async Task<bool> HasAdvancedSurfaceCapabilityAsync()
        {
            // Check if the agent has advanced surface generation capability
            return await _materialAgent.HasCapabilityAsync(CapabilityType.TextureGeneration);
        }

        private async Task<bool> HasVolumetricLightingCapabilityAsync()
        {
            // Check if the agent has volumetric lighting capability
            return await _materialAgent.HasCapabilityAsync(CapabilityType.ShaderGeneration);
        }
    }

    /// <summary>
    /// Interface for dynamic material generation
    /// </summary>
    public interface IDynamicMaterialGenerator
    {
        Task<MaterialGenerationResult> GenerateMaterialAsync(MaterialGenerationRequest request);
        Task<MaterialGenerationResult> GenerateMaterialBatchAsync(List<MaterialGenerationRequest> requests);
        Task<MaterialGenerationResult> RefineMaterialAsync(MaterialRefinementRequest request);
        Task<MaterialGenerationResult> GenerateMaterialVariationsAsync(MaterialVariationRequest request);
    }
}
