using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
using Nexo.Orchestration.Validation;
using Nexo.Orchestration.Models;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Routing;

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
        services.AddSingleton<LifecycleManager>();
        services.AddSingleton<HealthMonitor>();

        // Communication
        services.AddSingleton<IAgentBus, AgentBus>();
        services.AddSingleton<ChannelManager>();
        services.AddSingleton<MessageSchemaValidator>();
        // AgentBusNetworkBridge is registered by AddAgentBusNetworkBridge when INetworkBus is also registered (e.g. in API).

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

        // Orchestrator
        services.AddScoped<Orchestrator>();

        // Asset Storage (default to local storage)
        // Note: IAssetStorage should be registered by consuming application
        // services.AddSingleton<IAssetStorage, LocalAssetStorage>();

        // Build Tools
        // Note: IBuildTool should be registered by consuming application for build domains.

        // Playtest Services
        // Note: ITelemetryStore and IGameRunner should be registered by consuming application
        // services.AddSingleton<ITelemetryStore, InMemoryTelemetryStore>();

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
    /// Registers the bridge between IAgentBus and INetworkBus. Call after both AddNexoOrchestration and INetworkBus registration.
    /// </summary>
    public static IServiceCollection AddAgentBusNetworkBridge(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration != null)
            services.Configure<AgentBusNetworkBridgeOptions>(configuration.GetSection(AgentBusNetworkBridgeOptions.SectionName));
        else
            services.Configure<AgentBusNetworkBridgeOptions>(_ => { });
        services.AddHostedService<AgentBusNetworkBridge>();
        return services;
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

