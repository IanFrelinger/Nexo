using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Services
{
    /// <summary>
    /// Analyzes material generation requests to determine appropriate material properties
    /// </summary>
    public partial class MaterialContextAnalyzer : IMaterialContextAnalyzer
    {
        private readonly ILogger<MaterialContextAnalyzer> _logger;
        private readonly IColorPaletteAnalyzer _colorAnalyzer;
        private readonly ISurfaceTypeDetector _surfaceDetector;
        private readonly IPerformanceAnalyzer _performanceAnalyzer;

        public MaterialContextAnalyzer(
            ILogger<MaterialContextAnalyzer> logger,
            IColorPaletteAnalyzer colorAnalyzer,
            ISurfaceTypeDetector surfaceDetector,
            IPerformanceAnalyzer performanceAnalyzer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _colorAnalyzer = colorAnalyzer ?? throw new ArgumentNullException(nameof(colorAnalyzer));
            _surfaceDetector = surfaceDetector ?? throw new ArgumentNullException(nameof(surfaceDetector));
            _performanceAnalyzer = performanceAnalyzer ?? throw new ArgumentNullException(nameof(performanceAnalyzer));
        }

        public async Task<MaterialContext> AnalyzeAsync(MaterialRequest request)
        {
            _logger.LogInformation("Analyzing material request: {RequestId}", request.RequestId);

            try
            {
                // Extract context from natural language description
                var description = request.Description;
                var visualStyle = request.VisualStyle;
                var targetPlatform = request.TargetPlatform;

                // Analyze color palette from description
                var colorPalette = await _colorAnalyzer.AnalyzeColorPaletteAsync(description, visualStyle);

                // Detect surface type from description
                var surfaceType = await _surfaceDetector.DetectSurfaceTypeAsync(description);

                // Determine material type based on context
                var materialType = DetermineMaterialType(description, visualStyle, targetPlatform);

                // Extract surface details from description
                var surfaceDetails = await ExtractSurfaceDetailsAsync(description);

                // Determine lighting requirements
                var lightingRequirements = await DetermineLightingRequirementsAsync(description, visualStyle);

                // Calculate weathering level
                var weatheringLevel = CalculateWeatheringLevel(description);

                // Determine emission requirements
                var emissionRequired = DetermineEmissionRequirements(description);

                // Analyze performance constraints
                var performanceConstraints = await _performanceAnalyzer.AnalyzeConstraintsAsync(targetPlatform, materialType);

                var context = new MaterialContext
                {
                    RequestId = request.RequestId,
                    Description = description,
                    VisualStyle = visualStyle,
                    TargetPlatform = targetPlatform,
                    MaterialType = materialType,
                    ColorPalette = colorPalette,
                    SurfaceType = surfaceType,
                    SurfaceDetails = surfaceDetails,
                    LightingRequirements = lightingRequirements,
                    WeatheringLevel = weatheringLevel,
                    EmissionRequired = emissionRequired,
                    PerformanceConstraints = performanceConstraints,
                    Complexity = CalculateComplexity(surfaceDetails, lightingRequirements, weatheringLevel)
                };

                _logger.LogDebug("Material context analysis complete: {MaterialType}, {SurfaceType}, {Complexity}", 
                    materialType, surfaceType, context.Complexity);

                return context;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing material context for request: {RequestId}", request.RequestId);
                throw;
            }
        }

        private MaterialType DetermineMaterialType(string description, string visualStyle, PlatformType targetPlatform)
        {
            // Analyze description for material type indicators
            var lowerDescription = description.ToLowerInvariant();
            var lowerStyle = visualStyle.ToLowerInvariant();

            // Check for PBR material indicators
            if (lowerDescription.Contains("pbr") || 
                lowerDescription.Contains("physically based") ||
                lowerDescription.Contains("metallic") ||
                lowerDescription.Contains("roughness"))
            {
                return MaterialType.PBR;
            }

            // Check for transparent material indicators
            if (lowerDescription.Contains("transparent") ||
                lowerDescription.Contains("glass") ||
                lowerDescription.Contains("water") ||
                lowerDescription.Contains("crystal"))
            {
                return MaterialType.Transparent;
            }

            // Check for emissive material indicators
            if (lowerDescription.Contains("glow") ||
                lowerDescription.Contains("emissive") ||
                lowerDescription.Contains("light") ||
                lowerDescription.Contains("neon"))
            {
                return MaterialType.Emissive;
            }

            // Check for unlit material indicators
            if (lowerDescription.Contains("unlit") ||
                lowerDescription.Contains("flat") ||
                lowerDescription.Contains("cartoon") ||
                lowerDescription.Contains("stylized"))
            {
                return MaterialType.Unlit;
            }

            // Default based on platform capabilities
            return targetPlatform switch
            {
                PlatformType.Mobile => MaterialType.Unlit,
                PlatformType.Web => MaterialType.Unlit,
                _ => MaterialType.PBR
            };
        }

        private async Task<List<SurfaceDetail>> ExtractSurfaceDetailsAsync(string description)
        {
            var details = new List<SurfaceDetail>();
            var lowerDescription = description.ToLowerInvariant();

            // Extract scratch details
            if (lowerDescription.Contains("scratch") || lowerDescription.Contains("scratched"))
            {
                var intensity = ExtractIntensity(lowerDescription, "scratch");
                details.Add(new SurfaceDetail { Type = SurfaceDetailType.Scratches, Intensity = intensity });
            }

            // Extract dent details
            if (lowerDescription.Contains("dent") || lowerDescription.Contains("dented"))
            {
                var intensity = ExtractIntensity(lowerDescription, "dent");
                details.Add(new SurfaceDetail { Type = SurfaceDetailType.Dents, Intensity = intensity });
            }

            // Extract wear details
            if (lowerDescription.Contains("wear") || lowerDescription.Contains("worn"))
            {
                var intensity = ExtractIntensity(lowerDescription, "wear");
                details.Add(new SurfaceDetail { Type = SurfaceDetailType.Wear, Intensity = intensity });
            }

            // Extract rust details
            if (lowerDescription.Contains("rust") || lowerDescription.Contains("rusty"))
            {
                var intensity = ExtractIntensity(lowerDescription, "rust");
                details.Add(new SurfaceDetail { Type = SurfaceDetailType.Rust, Intensity = intensity });
            }

            return details;
        }

        private float ExtractIntensity(string description, string keyword)
        {
            // Look for intensity indicators in the description
            if (description.Contains("heavy") || description.Contains("severe") || description.Contains("extreme"))
                return 0.8f;
            if (description.Contains("moderate") || description.Contains("medium"))
                return 0.5f;
            if (description.Contains("light") || description.Contains("slight") || description.Contains("subtle"))
                return 0.2f;
            if (description.Contains("heavy") || description.Contains("severe"))
                return 0.9f;

            return 0.5f; // Default moderate intensity
        }

        private async Task<List<LightingRequirement>> DetermineLightingRequirementsAsync(string description, string visualStyle)
        {
            var requirements = new List<LightingRequirement>();
            var lowerDescription = description.ToLowerInvariant();
            var lowerStyle = visualStyle.ToLowerInvariant();

            // Check for dynamic lighting requirements
            if (lowerDescription.Contains("dynamic") || 
                lowerDescription.Contains("real-time") ||
                lowerStyle.Contains("realistic"))
            {
                requirements.Add(new LightingRequirement { Type = LightingType.Dynamic });
            }

            // Check for static lighting requirements
            if (lowerDescription.Contains("static") || 
                lowerDescription.Contains("baked") ||
                lowerStyle.Contains("stylized"))
            {
                requirements.Add(new LightingRequirement { Type = LightingType.Static });
            }

            // Check for volumetric lighting requirements
            if (lowerDescription.Contains("volumetric") || 
                lowerDescription.Contains("fog") ||
                lowerDescription.Contains("atmospheric"))
            {
                requirements.Add(new LightingRequirement { Type = LightingType.Volumetric });
            }

            return requirements;
        }

        private float CalculateWeatheringLevel(string description)
        {
            var lowerDescription = description.ToLowerInvariant();

            if (lowerDescription.Contains("pristine") || lowerDescription.Contains("new"))
                return 0.0f;
            if (lowerDescription.Contains("slightly worn") || lowerDescription.Contains("aged"))
                return 0.3f;
            if (lowerDescription.Contains("weathered") || lowerDescription.Contains("worn"))
                return 0.6f;
            if (lowerDescription.Contains("heavily weathered") || lowerDescription.Contains("damaged"))
                return 0.9f;

            return 0.5f; // Default moderate weathering
        }

        private bool DetermineEmissionRequirements(string description)
        {
            var lowerDescription = description.ToLowerInvariant();

            return lowerDescription.Contains("glow") ||
                   lowerDescription.Contains("emissive") ||
                   lowerDescription.Contains("light") ||
                   lowerDescription.Contains("neon") ||
                   lowerDescription.Contains("luminous");
        }

        private MaterialComplexity CalculateComplexity(List<SurfaceDetail> surfaceDetails, List<LightingRequirement> lightingRequirements, float weatheringLevel)
        {
            var complexity = MaterialComplexity.Low;

            // Increase complexity based on surface details
            if (surfaceDetails.Count > 2) complexity = MaterialComplexity.Medium;
            if (surfaceDetails.Count > 4) complexity = MaterialComplexity.High;

            // Increase complexity based on lighting requirements
            if (lightingRequirements.Any(r => r.Type == LightingType.Volumetric)) complexity = MaterialComplexity.High;
            if (lightingRequirements.Count > 2) complexity = MaterialComplexity.Medium;

            // Increase complexity based on weathering level
            if (weatheringLevel > 0.7f) complexity = MaterialComplexity.Medium;
            if (weatheringLevel > 0.9f) complexity = MaterialComplexity.High;

            return complexity;
        }
    }

    /// <summary>
    /// Interface for material context analysis
    /// </summary>
    public partial interface IMaterialContextAnalyzer
    {
        Task<MaterialContext> AnalyzeAsync(MaterialRequest request);
    }

    /// <summary>
    /// Interface for color palette analysis
    /// </summary>
    public partial interface IColorPaletteAnalyzer
    {
        Task<ColorPalette> AnalyzeColorPaletteAsync(string description, string visualStyle);
    }

    /// <summary>
    /// Interface for surface type detection
    /// </summary>
    public partial interface ISurfaceTypeDetector
    {
        Task<SurfaceType> DetectSurfaceTypeAsync(string description);
    }

    /// <summary>
    /// Interface for performance analysis
    /// </summary>
    public partial interface IPerformanceAnalyzer
    {
        Task<PerformanceConstraints> AnalyzeConstraintsAsync(PlatformType platform, MaterialType materialType);
    }
}
