using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;

namespace Nexo.Feature.AI.Services.EnhancedFeatureFactory.Analysis
{
    public partial class RequestAnalyzer
    {
        private readonly IModelOrchestrator _modelOrchestrator;
        private readonly ILogger _logger;

        public RequestAnalyzer(IModelOrchestrator modelOrchestrator, ILogger logger)
        {
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<RequestComplexityAnalysis> AnalyzeRequestComplexity(FeatureGenerationRequest request)
        {
            // Lightweight heuristic; real implementation could prompt an LLM via _modelOrchestrator
            var requiresSpecial = (request.TargetPlatforms?.Count ?? 0) > 1 ||
                                  (request.PerformanceRequirements?.TargetFps ?? 0) >= 120 ||
                                  (request.SecurityRequirements?.HardenedSandbox ?? false);

            var analysis = new RequestComplexityAnalysis
            {
                RequiresSpecializedCoordination = requiresSpecial,
                ComplexityLevel = requiresSpecial ? "High" : "Low",
                RequiredSpecializations = requiresSpecial ? new[] { "performance", "security", "platform" } : Array.Empty<string>(),
                EstimatedEffort = requiresSpecial ? 8 : 2
            };

            return Task.FromResult(analysis);
        }
    }
}


