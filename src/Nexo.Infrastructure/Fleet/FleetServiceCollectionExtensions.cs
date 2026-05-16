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
}
