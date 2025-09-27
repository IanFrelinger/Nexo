using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Application.Interfaces.Platform;
using Nexo.Feature.AI.Models;

namespace Nexo.Infrastructure.Services.Platform
{
    /// <summary>
    /// Metal shader generation functionality
    /// </summary>
    public partial class iOSCodeGenerator : IIOSCodeGenerator
    {
        /// <summary>
        /// Generates Metal shaders for graphics optimization.
        /// </summary>
        public async Task<IEnumerable<MetalShader>> GenerateMetalShadersAsync(
            ApplicationLogic applicationLogic,
            iOSGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            var shaders = new List<MetalShader>();

            try
            {
                // Generate Metal shaders for graphics-intensive features
                foreach (var feature in applicationLogic.Features.Where(f => f.RequiresGraphics))
                {
                    var shader = await GenerateMetalShaderForFeatureAsync(feature, options, cancellationToken);
                    shaders.Add(shader);
                }

                return shaders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Metal shaders");
                return shaders;
            }
        }

        private async Task<MetalShader> GenerateMetalShaderForFeatureAsync(
            Nexo.Core.Application.Interfaces.Platform.Feature feature,
            iOSGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var shader = new MetalShader
            {
                Name = $"{feature.Name}Shader",
                FeatureName = feature.Name,
                Description = feature.Description
            };

            try
            {
                // Generate Metal shader using AI
                var prompt = $@"
Generate a Metal shader for the following feature:
- Name: {feature.Name}
- Description: {feature.Description}
- Graphics Requirements: {feature.GraphicsRequirements}

Requirements:
- Use Metal Shading Language (MSL)
- Optimize for iOS GPU
- Include vertex and fragment shaders
- Add proper error handling
- Use modern Metal features
- Follow Apple's Metal guidelines

Generate complete, production-ready Metal shader code.
";

                var request = new ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                
                shader.Code = response.Response;
                shader.GeneratedAt = DateTimeOffset.UtcNow;
                shader.Success = true;

                return shader;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Metal shader for feature: {FeatureName}", feature.Name);
                shader.Success = false;
                shader.ErrorMessage = ex.Message;
                return shader;
            }
        }
    }
}
