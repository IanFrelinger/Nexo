using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Services
{
    /// <summary>
    /// Core material generation functionality
    /// </summary>
    public partial class DynamicMaterialGenerator : IDynamicMaterialGenerator
    {
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
}
