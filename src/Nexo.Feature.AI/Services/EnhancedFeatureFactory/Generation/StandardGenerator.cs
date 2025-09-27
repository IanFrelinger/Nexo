using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;

namespace Nexo.Feature.AI.Services.EnhancedFeatureFactory.Generation
{
    public class StandardGenerator
    {
        private readonly IModelOrchestrator _modelOrchestrator;
        private readonly ILogger _logger;

        public StandardGenerator(IModelOrchestrator modelOrchestrator, ILogger logger)
        {
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<FeatureGenerationResponse> GenerateStandardFeature(FeatureGenerationRequest request)
        {
            var prompt = $"Generate simple feature: {request.Description}";
            var response = await _modelOrchestrator.GenerateResponseAsync(prompt);
            if (!response.Success)
            {
                return new FeatureGenerationResponse
                {
                    Success = false,
                    ErrorMessage = "Standard generation failed",
                    GeneratedCode = string.Empty
                };
            }

            return new FeatureGenerationResponse
            {
                Success = true,
                GeneratedCode = response.Content
            };
        }
    }
}


