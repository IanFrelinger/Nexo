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
using Nexo.Feature.Factory.Domain.Models;
using Nexo.Feature.Factory.Domain.ValueObjects;

namespace Nexo.Feature.Factory.Application.Agents
{
    /// <summary>
    /// AI agent specialized in analyzing natural language descriptions and extracting domain entities, value objects, and business rules.
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public sealed partial class DomainAnalysisAgent : IAgent
    {
        private readonly IModelOrchestrator _modelOrchestrator;
        private readonly ILogger<DomainAnalysisAgent> _logger;
        private AgentState _status = AgentState.Idle;

        public string AgentId => "domain-analysis-agent";
        public string Name => "Domain Analysis Agent";
        public string Description => "Analyzes natural language descriptions to extract domain entities, value objects, and business rules";
        public AgentState Status => _status;

        public IReadOnlyList<AgentCapability> Capabilities => new List<AgentCapability>
        {
            new AgentCapability("EntityExtraction", "Extract entities from natural language", "string", "EntityDefinition[]"),
            new AgentCapability("ValueObjectExtraction", "Extract value objects from natural language", "string", "ValueObjectDefinition[]"),
            new AgentCapability("BusinessRuleExtraction", "Extract business rules from natural language", "string", "BusinessRule[]"),
            new AgentCapability("ValidationRuleExtraction", "Extract validation rules from natural language", "string", "ValidationRule[]")
        };

        public DomainAnalysisAgent(IModelOrchestrator modelOrchestrator, ILogger<DomainAnalysisAgent> logger)
        {
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            _status = AgentState.Idle;
            _logger.LogInformation("Domain Analysis Agent initialized");
            await Task.CompletedTask;
        }

        public async Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            _status = AgentState.Offline;
            _logger.LogInformation("Domain Analysis Agent shut down");
            await Task.CompletedTask;
        }

        public async Task<AgentResponse> ProcessAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _status = AgentState.Working;
                _logger.LogInformation("Processing domain analysis request: {RequestId}", request.RequestId);

                object result = request.RequestType switch
                {
                    "AnalyzeFeature" => await AnalyzeFeatureAsync(request.Data, cancellationToken),
                    "ExtractEntities" => await ExtractEntitiesAsync(request.Data, cancellationToken),
                    "ExtractValueObjects" => await ExtractValueObjectsAsync(request.Data, cancellationToken),
                    "ExtractBusinessRules" => await ExtractBusinessRulesAsync(request.Data, cancellationToken),
                    _ => throw new NotSupportedException($"Request type '{request.RequestType}' is not supported")
                };

                _status = AgentState.Idle;
                return new AgentResponse(request.RequestId, result, true);
            }
            catch (Exception ex)
            {
                _status = AgentState.Error;
                _logger.LogError(ex, "Error processing domain analysis request: {RequestId}", request.RequestId);
                return new AgentResponse(request.RequestId, new { Error = ex.Message }, false, ex.Message);
            }
        }
        // This class acts as an orchestrator for various domain analysis functionalities,
        // with specific categories defined in partial classes.
    }
}