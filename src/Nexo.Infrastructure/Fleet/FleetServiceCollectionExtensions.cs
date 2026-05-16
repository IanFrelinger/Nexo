using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexo.Core.Application.Fleet.Ports;

namespace Nexo.Infrastructure.Fleet;

/// <summary>
/// Phase 1 mesh director: in-memory fleet + task registry and placement.
/// </summary>
public static class FleetServiceCollectionExtensions
{
    public static IServiceCollection AddNexoFleetDirector(this IServiceCollection services)
    {
        services.TryAddSingleton<IFleetNodeRegistry, InMemoryFleetNodeRegistry>();
        services.TryAddSingleton<IMeshTaskRegistry, InMemoryMeshTaskRegistry>();
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
}
