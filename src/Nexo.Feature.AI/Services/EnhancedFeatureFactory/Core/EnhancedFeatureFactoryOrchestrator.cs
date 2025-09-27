using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Agents.Coordination;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Services.EnhancedFeatureFactory.Execution;
using Nexo.Feature.AI.Services.EnhancedFeatureFactory.Analysis;
using Nexo.Feature.AI.Services.EnhancedFeatureFactory.Generation;
using Nexo.Feature.AI.Services.EnhancedFeatureFactory.Processing;

namespace Nexo.Feature.AI.Services.EnhancedFeatureFactory.Core
{
    /// <summary>
    /// Orchestrator delegating enhanced feature generation to specialized components
    /// </summary>
    public class EnhancedFeatureFactoryOrchestrator : IEnhancedFeatureFactoryOrchestrator
    {
        private readonly ILogger<EnhancedFeatureFactoryOrchestrator> _logger;
        private readonly RequestAnalyzer _analyzer;
        private readonly CoordinationExecutor _coordinationExecutor;
        private readonly StandardGenerator _standardGenerator;
        private readonly ResponseExtractor _extractor;

        public EnhancedFeatureFactoryOrchestrator(
            IAgentCoordinator agentCoordinator,
            ISpecializedAgentRegistry agentRegistry,
            IModelOrchestrator modelOrchestrator,
            ILogger<EnhancedFeatureFactoryOrchestrator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _analyzer = new RequestAnalyzer(modelOrchestrator, logger);
            _coordinationExecutor = new CoordinationExecutor(agentCoordinator, agentRegistry, logger);
            _standardGenerator = new StandardGenerator(modelOrchestrator, logger);
            _extractor = new ResponseExtractor(logger);
        }

        public async Task<FeatureGenerationResponse> GenerateFeatureAsync(FeatureGenerationRequest request)
        {
            try
            {
                _logger.LogInformation("Starting enhanced feature generation for: {Description}", request.Description);

                var complexityAnalysis = await _analyzer.AnalyzeRequestComplexity(request);
                if (complexityAnalysis.RequiresSpecializedCoordination)
                {
                    _logger.LogInformation("Using specialized agent coordination for complex request");
                    return await GenerateWithSpecializedCoordination(request, complexityAnalysis);
                }

                _logger.LogInformation("Using standard feature generation for simple request");
                return await _standardGenerator.GenerateStandardFeature(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during enhanced feature generation");
                return new FeatureGenerationResponse
                {
                    Success = false,
                    ErrorMessage = $"Feature generation failed: {ex.Message}",
                    GeneratedCode = string.Empty
                };
            }
        }

        public async Task<FeatureGenerationResponse> GenerateWithSpecializedCoordination(
            FeatureGenerationRequest request,
            RequestComplexityAnalysis complexityAnalysis)
        {
            var complexRequest = _coordinationExecutor.BuildComplexAgentRequest(request, complexityAnalysis);
            var coordinatedResponse = await _coordinationExecutor.ExecuteAsync(complexRequest);
            if (!coordinatedResponse.Success)
            {
                return new FeatureGenerationResponse
                {
                    Success = false,
                    ErrorMessage = coordinatedResponse.ErrorMessage ?? "Specialized coordination failed",
                    GeneratedCode = string.Empty
                };
            }

            return _extractor.BuildFeatureGenerationResponseFrom(coordinatedResponse);
        }
    }
}


