using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Application.Interfaces;
using Nexo.Feature.Factory.Application.Agents;
using Nexo.Feature.Factory.Domain.Entities;
using Nexo.Feature.Factory.Domain.Enums;

namespace Nexo.Feature.Factory.Application.Services
{
    /// <summary>
    /// Coordinates multiple specialized AI agents to work together on feature generation.
    /// </summary>
    public sealed class AgentCoordinator : IAgentCoordinator
    {
        private readonly Dictionary<string, IAgent> _agents = new();
        private readonly ILogger<AgentCoordinator> _logger;

        public AgentCoordinator(ILogger<AgentCoordinator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<FeatureSpecification> CoordinateAnalysisAsync(FeatureSpecification specification, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Coordinating analysis for specification: {SpecificationId}", specification.Id);

            try
            {
                // Find domain analysis agent
                var domainAnalysisAgent = _agents.Values.OfType<DomainAnalysisAgent>().FirstOrDefault();
                if (domainAnalysisAgent == null)
                {
                    throw new InvalidOperationException("Domain Analysis Agent not found");
                }

                // Create analysis request
                var request = new AgentRequest(
                    Guid.NewGuid().ToString(),
                    "AnalyzeFeature",
                    (specification.Description, specification.TargetPlatform)
                );

                // Process the request
                var response = await domainAnalysisAgent.ProcessAsync(request, cancellationToken);
                
                if (!response.IsSuccess)
                {
                    throw new InvalidOperationException($"Domain analysis failed: {response.ErrorMessage}");
                }

                var analyzedSpecification = response.Data as FeatureSpecification;
                if (analyzedSpecification == null)
                {
                    throw new InvalidOperationException("Invalid response from domain analysis agent");
                }

                analyzedSpecification.UpdateStatus(FeatureSpecificationStatus.Analyzing);
                
                _logger.LogInformation("Analysis coordination completed for specification: {SpecificationId}", specification.Id);
                return analyzedSpecification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error coordinating analysis for specification: {SpecificationId}", specification.Id);
                specification.UpdateStatus(FeatureSpecificationStatus.Error);
                throw;
            }
        }

        public async Task<IReadOnlyList<CodeArtifact>> CoordinateGenerationAsync(FeatureSpecification specification, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Coordinating generation for specification: {SpecificationId}", specification.Id);

            try
            {
                specification.UpdateStatus(FeatureSpecificationStatus.Generating);

                // Find code generation agent
                var codeGenerationAgent = _agents.Values.OfType<CodeGenerationAgent>().FirstOrDefault();
                if (codeGenerationAgent == null)
                {
                    throw new InvalidOperationException("Code Generation Agent not found");
                }

                // Create generation request
                var request = new AgentRequest(
                    Guid.NewGuid().ToString(),
                    "GenerateAll",
                    specification
                );

                // Process the request
                var response = await codeGenerationAgent.ProcessAsync(request, cancellationToken);
                
                if (!response.IsSuccess)
                {
                    throw new InvalidOperationException($"Code generation failed: {response.ErrorMessage}");
                }

                var artifacts = response.Data as List<CodeArtifact>;
                if (artifacts == null)
                {
                    throw new InvalidOperationException("Invalid response from code generation agent");
                }

                _logger.LogInformation("Generation coordination completed for specification: {SpecificationId}. Generated {ArtifactCount} artifacts", 
                    specification.Id, artifacts.Count);
                
                return artifacts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error coordinating generation for specification: {SpecificationId}", specification.Id);
                specification.UpdateStatus(FeatureSpecificationStatus.Error);
                throw;
            }
        }

        public Task<ValidationResult> CoordinateValidationAsync(IReadOnlyList<CodeArtifact> artifacts, FeatureSpecification specification, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Coordinating validation for {ArtifactCount} artifacts", artifacts.Count);

            try
            {
                var errors = new List<ValidationError>();
                var warnings = new List<ValidationWarning>();

                // Basic validation - check for required artifacts
                var requiredArtifacts = new[] { ArtifactType.Entity, ArtifactType.Repository, ArtifactType.UseCase, ArtifactType.Test };
                
                foreach (var artifactType in requiredArtifacts)
                {
                    if (!artifacts.Any(a => a.Type == artifactType))
                    {
                        warnings.Add(new ValidationWarning(
                            $"No {artifactType} artifacts found"
                        ));
                    }
                }

                // Validate artifact content
                foreach (var artifact in artifacts)
                {
                    if (string.IsNullOrWhiteSpace(artifact.Content))
                    {
                        errors.Add(new ValidationError(
                            $"Empty content in artifact: {artifact.Name}",
                            artifact.Name,
                            null
                        ));
                    }

                    // Basic syntax validation for C# files
                    if (artifact.Name.EndsWith(".cs"))
                    {
                        if (!artifact.Content.Contains("namespace") && !string.IsNullOrEmpty(artifact.Namespace))
                        {
                            warnings.Add(new ValidationWarning(
                                $"Missing namespace declaration in {artifact.Name}",
                                artifact.Name,
                                null
                            ));
                        }

                        if (!artifact.Content.Contains("class") && !artifact.Content.Contains("interface"))
                        {
                            errors.Add(new ValidationError(
                                $"No class or interface found in {artifact.Name}",
                                artifact.Name
                            ));
                        }
                    }
                }

                var isValid = errors.Count == 0;
                
                _logger.LogInformation("Validation coordination completed. Valid: {IsValid}, Errors: {ErrorCount}, Warnings: {WarningCount}", 
                    isValid, errors.Count, warnings.Count);

                return Task.FromResult(new ValidationResult(isValid, errors, warnings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error coordinating validation");
                return Task.FromResult(new ValidationResult(false, new List<ValidationError> 
                { 
                    new ValidationError($"Validation coordination failed: {ex.Message}") 
                }));
            }
        }

        public async Task<IReadOnlyList<AgentStatus>> GetAgentStatusesAsync()
        {
            var statuses = new List<AgentStatus>();
            
            foreach (var agent in _agents.Values)
            {
                var status = new AgentStatus(
                    agent.AgentId,
                    agent.Name,
                    agent.Status,
                    DateTimeOffset.UtcNow
                );
                statuses.Add(status);
            }

            return await Task.FromResult(statuses.AsReadOnly());
        }

        public async Task RegisterAgentAsync(IAgent agent, CancellationToken cancellationToken = default)
        {
            if (agent == null) throw new ArgumentNullException(nameof(agent));

            _agents[agent.AgentId] = agent;
            await agent.InitializeAsync(cancellationToken);
            
            _logger.LogInformation("Registered agent: {AgentId} - {AgentName}", agent.AgentId, agent.Name);
        }

        public async Task UnregisterAgentAsync(string agentId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));

            if (_agents.TryGetValue(agentId, out var agent))
            {
                await agent.ShutdownAsync(cancellationToken);
                _agents.Remove(agentId);
                
                _logger.LogInformation("Unregistered agent: {AgentId}", agentId);
            }
        }
    }
}
