using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Services
{
    /// <summary>
    /// Material variation generation functionality
    /// </summary>
    public partial class DynamicMaterialGenerator : IDynamicMaterialGenerator
    {
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
    }
}
