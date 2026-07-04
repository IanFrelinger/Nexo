using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexo.Commercial.Fleet.Contracts.Ports;
using Nexo.Commercial.Fleet.Infrastructure.Communication;
using Nexo.Commercial.Fleet.Infrastructure.MeshLab;

namespace Nexo.Commercial.Fleet.Infrastructure;

/// <summary>
/// Phase 1 mesh director: in-memory fleet + task registry and placement.
/// </summary>
public static class FleetServiceCollectionExtensions
{
    /// <summary>Add nexo fleet director operation.</summary>
    /// <summary>Registers Phase 1 mesh director services and background workers.</summary>
    public static IServiceCollection AddNexoFleetDirector(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        var persistence = new MeshPersistenceOptions();
        if (configuration is not null)
        {
            services.AddOptions<MeshPlacementTrustOptions>()
                .Bind(configuration.GetSection(MeshPlacementTrustOptions.SectionPath));
            services.AddOptions<MeshFleetRegistrationOptions>()
                .Bind(configuration.GetSection(MeshFleetRegistrationOptions.SectionPath));
            services.AddOptions<MeshPersistenceOptions>()
                .Bind(configuration.GetSection(MeshPersistenceOptions.SectionPath));
            configuration.GetSection(MeshPersistenceOptions.SectionPath).Bind(persistence);
        }
        else
        {
            services.AddOptions<MeshPlacementTrustOptions>();
            services.AddOptions<MeshFleetRegistrationOptions>();
            services.AddOptions<MeshPersistenceOptions>();
        }

        if (!MeshPersistenceOptions.IsKnownProvider(persistence.Provider))
        {
            throw new InvalidOperationException(
                $"Unknown mesh persistence provider '{persistence.Provider}'. Supported: InMemory, LiteDb.");
        }

        if (MeshPersistenceOptions.IsLiteDb(persistence.Provider))
        {
            var dbPath = string.IsNullOrWhiteSpace(persistence.DatabasePath)
                ? "mesh-director.db"
                : persistence.DatabasePath.Trim();
            services.TryAddSingleton<IFleetNodeRegistry>(_ => new LiteDbFleetNodeRegistry(dbPath));
            services.TryAddSingleton<IMeshTaskRegistry>(_ => new LiteDbMeshTaskRegistry(dbPath));
        }
        else
        {
            services.TryAddSingleton<IFleetNodeRegistry, InMemoryFleetNodeRegistry>();
            services.TryAddSingleton<IMeshTaskRegistry, InMemoryMeshTaskRegistry>();
        }
        services.TryAddSingleton<IMeshTaskPlacementService, MeshTaskPlacementService>();
        return services;
    }

    /// <summary>
    /// Phase 5: optional periodic re-placement of stale pending mesh tasks.
    /// </summary>
    public static IServiceCollection AddNexoMeshElasticScheduling(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MeshElasticSchedulingOptions>()
            .Bind(configuration.GetSection(MeshElasticSchedulingOptions.SectionPath));
        services.AddHostedService<MeshPendingTaskRebalancerBackgroundService>();
        return services;
    }

    /// <summary>
    /// Phase 6: execution leases, migrate-for-checkpoint, optional lease sweep.
    /// </summary>
    public static IServiceCollection AddNexoMeshCheckpointScheduling(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MeshCheckpointOptions>()
            .Bind(configuration.GetSection(MeshCheckpointOptions.SectionPath));
        services.TryAddSingleton<MeshTaskExecutionService>();
        services.AddHostedService<MeshLeaseSweepBackgroundService>();
        return services;
    }

    /// <summary>
    /// Phase 4: mesh knowledge export/import and optional peer pull (requires adaptation + pattern stores).
    /// </summary>
    public static IServiceCollection AddNexoMeshKnowledgeReplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<MeshPeerKnowledgeSyncOptions>()
            .Bind(configuration.GetSection(MeshPeerKnowledgeSyncOptions.SectionPath));
        services.TryAddSingleton<MeshKnowledgeExportService>();
        services.TryAddSingleton<MeshKnowledgeImportService>();
        services.AddHostedService<MeshPeerKnowledgePullBackgroundService>();
        return services;
    }

    /// <summary>
    /// Virtual mesh lab: optional background worker that completes assigned tasks via the director HTTP API.
    /// </summary>
    public static IServiceCollection AddNexoMeshLabWorkerExecutor(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<MeshLabWorkerExecutorOptions>()
            .Bind(configuration.GetSection(MeshLabWorkerExecutorOptions.SectionPath));

        var enabled = configuration.GetValue(
            $"{MeshLabWorkerExecutorOptions.SectionPath}:Enabled",
            defaultValue: false);
        if (!enabled)
            return services;

        services.AddHttpClient(MeshLabWorkerExecutorClient.HttpClientName);
        services.TryAddSingleton<MeshLabWorkerExecutorClient>();
        services.AddHostedService<MeshLabWorkerExecutorBackgroundService>();
        return services;
    }

    /// <summary>
    /// Commercial fleet networking: HTTP network bus, knowledge sync, plasticity, and optional agent-bus bridge.
    /// Call after <c>AddNexoOrchestration</c> when cross-node networking is required.
    /// </summary>
    public static IServiceCollection AddNexoCommercialFleetNetworking(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        if (configuration is not null)
        {
            services.AddOptions<AgentBusNetworkBridgeOptions>()
                .Bind(configuration.GetSection(AgentBusNetworkBridgeOptions.SectionName));
        }
        else
        {
            services.AddOptions<AgentBusNetworkBridgeOptions>();
        }

        services.AddHostedService<AgentBusNetworkBridge>();
        return services;
    }

    /// <summary>
    /// Registers the bridge between <see cref="Nexo.Orchestration.Communication.IAgentBus"/> and <see cref="Nexo.Commercial.Fleet.Contracts.Networking.Ports.INetworkBus"/>.
    /// </summary>
    public static IServiceCollection AddAgentBusNetworkBridge(
        this IServiceCollection services,
        IConfiguration? configuration = null)
        => services.AddNexoCommercialFleetNetworking(configuration);
}
