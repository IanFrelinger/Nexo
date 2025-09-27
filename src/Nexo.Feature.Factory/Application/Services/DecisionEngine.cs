using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.Factory.Application.Interfaces;
using Nexo.Feature.Factory.Domain.Entities;
using Nexo.Feature.Factory.Domain.Enums;

namespace Nexo.Feature.Factory.Application.Services
{
    /// <summary>
    /// AI-powered decision engine that analyzes feature requirements and chooses the optimal execution strategy.
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public sealed partial class DecisionEngine : IDecisionEngine
    {
        private readonly IModelOrchestrator _modelOrchestrator;
        private readonly ILogger<DecisionEngine> _logger;

        public DecisionEngine(IModelOrchestrator modelOrchestrator, ILogger<DecisionEngine> logger)
        {
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ExecutionStrategyDecision> DetermineStrategyAsync(FeatureSpecification specification, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Determining execution strategy for specification: {SpecificationId}", specification.Id);

            try
            {
                // Analyze complexity
                var complexityAnalysis = await AnalyzeComplexityAsync(specification, cancellationToken);
                
                // Analyze performance requirements
                var performanceAnalysis = await AnalyzePerformanceRequirementsAsync(specification, cancellationToken);
                
                // Analyze platform optimizations
                var platformOptimizations = await AnalyzePlatformOptimizationsAsync(specification, cancellationToken);

                // Determine strategy based on analysis
                var strategy = DetermineStrategyFromAnalysis(complexityAnalysis, performanceAnalysis, platformOptimizations);
                var confidence = CalculateConfidence(complexityAnalysis, performanceAnalysis, platformOptimizations);
                var reasoning = GenerateReasoning(complexityAnalysis, performanceAnalysis, platformOptimizations, strategy);
                var factors = GenerateDecisionFactors(complexityAnalysis, performanceAnalysis, platformOptimizations);

                var decision = new ExecutionStrategyDecision(strategy, confidence, reasoning, factors);
                
                _logger.LogInformation("Execution strategy determined: {Strategy} (confidence: {Confidence})", strategy, confidence);
                
                return decision;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining execution strategy for specification: {SpecificationId}", specification.Id);
                
                // Fallback to generated strategy
                return new ExecutionStrategyDecision(
                    ExecutionStrategy.Generated,
                    0.5,
                    "Fallback to generated strategy due to analysis error",
                    new List<DecisionFactor>
                    {
                        new DecisionFactor("Error", 1.0, FactorImpact.Negative, "Analysis failed, using safe fallback")
                    }
                );
            }
        }
        // This class acts as an orchestrator for various decision engine functionalities,
        // with specific categories defined in partial classes.
    }
}