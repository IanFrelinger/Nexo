using Microsoft.Extensions.DependencyInjection;
using Nexo.Abstractions;
using Nexo.Core.Application.Common.Ports;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Architect;
using Nexo.Orchestration.Architect.Parsers;
using Nexo.Orchestration.Architect.Prompts;
using Nexo.Orchestration.Communication;
using Nexo.Orchestration.Coordination;
using Nexo.Orchestration.Coordination.Conflicts;
using Nexo.Orchestration.Coordination.ErrorRecovery;
using Nexo.Orchestration.Negotiation;
using Nexo.Orchestration.Validation;

namespace Nexo.Orchestration;

/// <summary>
/// Extension methods for registering orchestration services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all orchestration services to the service collection.
    /// </summary>
    public static IServiceCollection AddNexoOrchestration(this IServiceCollection services)
    {
        // Architect
        services.AddSingleton<DomainRecognizer>();
        services.AddSingleton<DecompositionRetriever>();
        services.AddSingleton<DecompositionPromptBuilder>();
        services.AddSingleton<DecompositionJsonParser>();
        services.AddSingleton<IArchitectAgent, ArchitectAgent>();

        // Validation
        services.AddSingleton<IValidator, SchemaValidator>();
        services.AddSingleton<IValidator, DependencyAnalyzer>();
        services.AddSingleton<IValidator, CoverageChecker>();
        services.AddSingleton<IValidator, ConstraintSolver>();

        // Agents
        services.AddSingleton<AgentFactory>();
        services.AddSingleton<LifecycleManager>();
        services.AddSingleton<HealthMonitor>();

        // Communication
        services.AddSingleton<IAgentBus, AgentBus>();
        services.AddSingleton<ChannelManager>();
        services.AddSingleton<MessageSchemaValidator>();

        // Coordination
        services.AddSingleton<DependencyResolver>();
        services.AddSingleton<ConflictDetector>();
        services.AddSingleton<ResourceAllocator>();
        services.AddSingleton<ProgressTracker>();
        services.AddSingleton<EscalationManager>();
        services.AddSingleton<OutputIntegrator>();
        services.AddSingleton<TimeoutManager>();
        services.AddSingleton<ErrorRecoveryManager>();

        // Negotiation
        services.AddSingleton<SchemaAdapter>();
        services.AddSingleton<ParetoOptimizer>();
        services.AddSingleton<ConstraintRelaxer>();
        services.AddSingleton<SynthesisEngine>();
        services.AddSingleton<NegotiationProtocol>();

        // Orchestrator
        services.AddSingleton<Orchestrator>();

        return services;
    }
}

