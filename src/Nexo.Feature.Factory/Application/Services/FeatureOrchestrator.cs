using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Application.Interfaces;
using Nexo.Feature.Factory.Domain.Entities;
using Nexo.Feature.Factory.Domain.Enums;
using Nexo.Feature.Factory.Domain.ValueObjects;

namespace Nexo.Feature.Factory.Application.Services
{
    /// <summary>
    /// Orchestrates the entire feature generation process from natural language to production-ready code.
    /// </summary>
    public sealed class FeatureOrchestrator : IFeatureOrchestrator
    {
        private readonly IAgentCoordinator _agentCoordinator;
        private readonly IDecisionEngine _decisionEngine;
        private readonly ILogger<FeatureOrchestrator> _logger;

        public FeatureOrchestrator(
            IAgentCoordinator agentCoordinator,
            IDecisionEngine decisionEngine,
            ILogger<FeatureOrchestrator> logger)
        {
            _agentCoordinator = agentCoordinator ?? throw new ArgumentNullException(nameof(agentCoordinator));
            _decisionEngine = decisionEngine ?? throw new ArgumentNullException(nameof(decisionEngine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<FeatureGenerationResult> GenerateFeatureAsync(string description, TargetPlatform targetPlatform, CancellationToken cancellationToken = default)
        {
            var startTime = DateTimeOffset.UtcNow;
            _logger.LogInformation("Starting feature generation for: {Description}", description);

            try
            {
                // Step 1: Analyze the feature description
                var specification = await AnalyzeFeatureAsync(description, targetPlatform, cancellationToken);
                
                // Step 2: Generate the feature
                var result = await GenerateFeatureAsync(specification, cancellationToken);
                
                var endTime = DateTimeOffset.UtcNow;
                var metadata = new GenerationMetadata(startTime, endTime, result.Metadata.AgentsUsed, result.Metadata.ExecutionStrategy);
                
                return new FeatureGenerationResult(
                    specification,
                    result.CodeArtifacts,
                    metadata,
                    result.IsSuccess,
                    result.Errors
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating feature: {Description}", description);
                var endTime = DateTimeOffset.UtcNow;
                var metadata = new GenerationMetadata(startTime, endTime, 0, ExecutionStrategy.Generated);
                
                return new FeatureGenerationResult(
                    new FeatureSpecification(new FeatureSpecificationId("error"), "Error during generation", TargetPlatform.DotNet),
                    new List<CodeArtifact>(),
                    metadata,
                    false,
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<FeatureGenerationResult> GenerateFeatureAsync(FeatureSpecification specification, CancellationToken cancellationToken = default)
        {
            var startTime = DateTimeOffset.UtcNow;
            _logger.LogInformation("Starting feature generation for specification: {SpecificationId}", specification.Id);

            try
            {
                // Step 1: Determine execution strategy
                var strategyDecision = await _decisionEngine.DetermineStrategyAsync(specification, cancellationToken);
                specification.SetExecutionStrategy(strategyDecision.Strategy);
                
                _logger.LogInformation("Execution strategy determined: {Strategy} (confidence: {Confidence})", 
                    strategyDecision.Strategy, strategyDecision.Confidence);

                // Step 2: Coordinate analysis if needed
                if (specification.Status == FeatureSpecificationStatus.Draft)
                {
                    specification = await _agentCoordinator.CoordinateAnalysisAsync(specification, cancellationToken);
                }

                // Step 3: Generate code artifacts
                var codeArtifacts = await _agentCoordinator.CoordinateGenerationAsync(specification, cancellationToken);
                
                // Step 4: Validate generated code
                var validationResult = await _agentCoordinator.CoordinateValidationAsync(codeArtifacts, specification, cancellationToken);
                
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.Message).ToList();
                    _logger.LogWarning("Validation failed with {ErrorCount} errors", errors.Count);
                    
                    var validationEndTime = DateTimeOffset.UtcNow;
                    var metadata = new GenerationMetadata(startTime, validationEndTime, 0, strategyDecision.Strategy);
                    
                    return new FeatureGenerationResult(
                        specification,
                        codeArtifacts,
                        metadata,
                        false,
                        errors
                    );
                }

                // Step 5: Update specification status
                specification.UpdateStatus(FeatureSpecificationStatus.Complete);

                var endTime = DateTimeOffset.UtcNow;
                var finalMetadata = new GenerationMetadata(startTime, endTime, 2, strategyDecision.Strategy); // DomainAnalysis + CodeGeneration agents
                
                _logger.LogInformation("Feature generation completed successfully. Generated {ArtifactCount} artifacts", codeArtifacts.Count);
                
                return new FeatureGenerationResult(
                    specification,
                    codeArtifacts,
                    finalMetadata,
                    true
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating feature for specification: {SpecificationId}", specification.Id);
                var endTime = DateTimeOffset.UtcNow;
                var metadata = new GenerationMetadata(startTime, endTime, 0, ExecutionStrategy.Generated);
                
                return new FeatureGenerationResult(
                    specification,
                    new List<CodeArtifact>(),
                    metadata,
                    false,
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<FeatureSpecification> AnalyzeFeatureAsync(string description, TargetPlatform targetPlatform, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Analyzing feature description for platform: {Platform}", targetPlatform);

            try
            {
                // Create initial specification
                var specification = new FeatureSpecification(
                    FeatureSpecificationId.New(),
                    description,
                    targetPlatform
                );

                // Use agent coordinator to analyze the feature
                var analyzedSpecification = await _agentCoordinator.CoordinateAnalysisAsync(specification, cancellationToken);
                
                _logger.LogInformation("Feature analysis completed. Found {EntityCount} entities, {ValueObjectCount} value objects, {BusinessRuleCount} business rules",
                    analyzedSpecification.Entities.Count,
                    analyzedSpecification.ValueObjects.Count,
                    analyzedSpecification.BusinessRules.Count);

                return analyzedSpecification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing feature: {Description}", description);
                throw;
            }
        }
    }
}
