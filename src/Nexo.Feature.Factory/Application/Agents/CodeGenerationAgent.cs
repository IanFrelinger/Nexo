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

namespace Nexo.Feature.Factory.Application.Agents
{
    /// <summary>
    /// AI agent specialized in generating Clean Architecture code from domain specifications.
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public sealed partial class CodeGenerationAgent : IAgent
    {
        private readonly IModelOrchestrator _modelOrchestrator;
        private readonly ILogger<CodeGenerationAgent> _logger;
        private AgentState _status = AgentState.Idle;

        public string AgentId => "code-generation-agent";
        public string Name => "Code Generation Agent";
        public string Description => "Generates Clean Architecture code from domain specifications";
        public AgentState Status => _status;

        public IReadOnlyList<AgentCapability> Capabilities => new List<AgentCapability>
        {
            new AgentCapability("EntityGeneration", "Generate entity classes", "EntityDefinition", "string"),
            new AgentCapability("ValueObjectGeneration", "Generate value object classes", "ValueObjectDefinition", "string"),
            new AgentCapability("RepositoryGeneration", "Generate repository interfaces and implementations", "EntityDefinition", "string[]"),
            new AgentCapability("UseCaseGeneration", "Generate use case classes", "EntityDefinition", "string[]"),
            new AgentCapability("TestGeneration", "Generate unit tests", "EntityDefinition", "string[]")
        };

        public CodeGenerationAgent(IModelOrchestrator modelOrchestrator, ILogger<CodeGenerationAgent> logger)
        {
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            _status = AgentState.Idle;
            _logger.LogInformation("Code Generation Agent initialized");
            await Task.CompletedTask;
        }

        public async Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            _status = AgentState.Offline;
            _logger.LogInformation("Code Generation Agent shut down");
            await Task.CompletedTask;
        }

        public async Task<AgentResponse> ProcessAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _status = AgentState.Working;
                _logger.LogInformation("Processing code generation request: {RequestId}", request.RequestId);

                object result = request.RequestType switch
                {
                    "GenerateEntity" => await GenerateEntityAsync(request.Data, cancellationToken),
                    "GenerateValueObject" => await GenerateValueObjectAsync(request.Data, cancellationToken),
                    "GenerateRepository" => await GenerateRepositoryAsync(request.Data, cancellationToken),
                    "GenerateUseCase" => await GenerateUseCaseAsync(request.Data, cancellationToken),
                    "GenerateTests" => await GenerateTestsAsync(request.Data, cancellationToken),
                    "GenerateAll" => await GenerateAllAsync(request.Data, cancellationToken),
                    _ => throw new NotSupportedException($"Request type '{request.RequestType}' is not supported")
                };

                _status = AgentState.Idle;
                return new AgentResponse(request.RequestId, result, true);
            }
            catch (Exception ex)
            {
                _status = AgentState.Error;
                _logger.LogError(ex, "Error processing code generation request: {RequestId}", request.RequestId);
                return new AgentResponse(request.RequestId, new { Error = ex.Message }, false, ex.Message);
            }
        }
        // This class acts as an orchestrator for various code generation functionalities,
        // with specific categories defined in partial classes.
    }
}