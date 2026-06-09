using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Abstractions;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Common.Services;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Architect;
using Nexo.Orchestration.Architect.Parsers;
using Nexo.Orchestration.Architect.Prompts;
using Nexo.Orchestration.Communication;
using Nexo.Orchestration.Coordination;
using Nexo.Orchestration.Coordination.Conflicts;
using Nexo.Orchestration.Coordination.ErrorRecovery;
using Nexo.Orchestration.Metrics;
using Nexo.Orchestration.Negotiation;
using Nexo.Orchestration.Barriers;
using Nexo.Orchestration.Routing;
using Nexo.Orchestration.Resources;
using Nexo.Orchestration.Transport;
using Nexo.Orchestration.Validation;
using Nexo.Orchestration.Models;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Routing;
using Nexo.Abstractions.Transport;

namespace Nexo.Orchestration;

/// <summary>
/// Extension methods for registering orchestration services.
/// 
/// Provides dependency injection registration for all orchestration components:
/// - Architect services (decomposition, validation)
/// - Agent services (factory, lifecycle, health)
/// - Communication services (message bus, channels)
/// - Coordination services (dependency resolution, conflict detection, etc.)
/// - Negotiation services (schema adapter, Pareto optimizer, etc.)
/// - Metrics and orchestrator
/// 
/// Call AddNexoOrchestration() to register all orchestration services.
/// Register optional <c>IAgentTransportInvocationHook</c> implementations (singleton or scoped) to extend transport invocations.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all orchestration services to the service collection.
    /// </summary>
    public static IServiceCollection AddNexoOrchestration(this IServiceCollection services)
    {
        services.AddSingleton<IOrchestrationRuntimeSpecAccessor, OrchestrationRuntimeSpecAccessor>();

        // Loop kernel (hot paths use this; default is sequential unless host decorates).
        services.TryAddSingleton<ILoopKernel, SequentialLoopKernel>();

        // Architect
        services.AddSingleton<DomainRecognizer>();
        services.AddSingleton<DecompositionRetriever>();
        services.AddSingleton<DecompositionPromptBuilder>();
        services.AddSingleton<DecompositionJsonParser>();
        services.AddSingleton<IArchitectAgent, ArchitectAgent>();
        services.TryAddSingleton<IEndpointRegistry, EmptyEndpointRegistry>();
        services.AddSingleton<IEndpointRouter, CompositeEndpointRouter>();
        services.AddSingleton<IRoutingPolicy, CapabilityRoutingPolicy>();
        services.AddSingleton<IRoutingPolicy, BarrierRoutingPolicy>();
        services.AddSingleton<IRoutingPolicy, GeographicAffinityPolicy>();
        services.AddSingleton<IRoutingPolicy, HealthFilterPolicy>();
        services.AddSingleton<IRoutingPolicy, PrioritySelectionPolicy>();
        services.TryAddSingleton<IBarrierAuditLog, NoopBarrierAuditLog>();

        // Validation
        services.AddSingleton<IValidator, SchemaValidator>();
        services.AddSingleton<IValidator, DependencyAnalyzer>();
        services.AddSingleton<IValidator, CoverageChecker>();
        services.AddSingleton<IValidator, ConstraintSolver>();

        // Agents
        services.AddSingleton<AgentFactory>();
        services.AddSingleton<IAgentRuntimeFactory>(sp => sp.GetRequiredService<AgentFactory>());
        services.AddSingleton<LifecycleManager>();
        services.AddSingleton<HealthMonitor>();

        // Unified provisioned resources (scoped per orchestration scope / request)
        services.AddOptions<OrchestrationResourceOptions>();
        services.AddScoped(sp => new OrchestrationResourceScope(
            sp.GetRequiredService<IOptions<OrchestrationResourceOptions>>().Value.TeardownPolicy,
            sp.GetService<ILogger<OrchestrationResourceScope>>()));

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

        // Metrics
        services.AddSingleton<OrchestrationMetrics>();

        // Orchestrator (resolve invocation hooks from DI so hosts can add policy/metadata pipelines)
        services.AddScoped(sp => new Orchestrator(
            sp.GetRequiredService<IArchitectAgent>(),
            sp.GetRequiredService<AgentFactory>(),
            sp.GetRequiredService<LifecycleManager>(),
            sp.GetRequiredService<DependencyResolver>(),
            sp.GetRequiredService<ConflictDetector>(),
            sp.GetRequiredService<ResourceAllocator>(),
            sp.GetRequiredService<ProgressTracker>(),
            sp.GetRequiredService<EscalationManager>(),
            sp.GetRequiredService<OutputIntegrator>(),
            sp.GetRequiredService<IAgentBus>(),
            sp.GetRequiredService<IAgentTransport>(),
            sp.GetRequiredService<ILoopKernel>(),
            sp.GetRequiredService<ILogger<Orchestrator>>(),
            negotiationProtocol: sp.GetService<NegotiationProtocol>(),
            metrics: sp.GetService<OrchestrationMetrics>(),
            runtimeSpecAccessor: sp.GetService<IOrchestrationRuntimeSpecAccessor>(),
            barrierContextAccessor: sp.GetService<IBarrierContextAccessor>(),
            barrierHierarchy: sp.GetService<BarrierHierarchy>(),
            barrierAuditLog: sp.GetService<IBarrierAuditLog>(),
            barrierOptions: sp.GetService<IOptions<BarrierOptions>>(),
            invocationHooks: sp.GetServices<IAgentTransportInvocationHook>()));

        return services;
    }

    private sealed class EmptyEndpointRegistry : IEndpointRegistry
    {
        public IReadOnlyList<EndpointDescriptor> GetAll() => Array.Empty<EndpointDescriptor>();
        public void Register(EndpointDescriptor descriptor) { }
        public void UpdateHealth(string endpoint, bool isHealthy) { }
    }

    private sealed class NoopBarrierAuditLog : IBarrierAuditLog
    {
        public ValueTask RecordAsync(BarrierAuditEvent auditEvent, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }

    /// <summary>
    /// Adds playtest services to the service collection.
    /// </summary>
    public static IServiceCollection AddPlaytestServices(this IServiceCollection services)
    {
        // Telemetry store (default to in-memory for testing)
        // services.AddSingleton<ITelemetryStore, InMemoryTelemetryStore>();

        // Game runner (must be implemented per game engine)
        // services.AddSingleton<IGameRunner, HeadlessGameRunner>();

        return services;
    }

    /// <summary>
    /// Adds asset generation services to the service collection.
    /// Asset adapters (3D models, images, audio) can be added later via extension packages.
    /// </summary>
    public static IServiceCollection AddAssetGenerators(this IServiceCollection services)
    {
        // Placeholder implementations are registered in the Adapters project
        // This method is kept for backward compatibility
        return services;
    }
}

