using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexo.Core.Application.Environments;
using Nexo.Core.Application.Environments.Ports;

namespace Nexo.Infrastructure.Environments;

/// <summary>
/// Registers pluggable map data providers (vector, terrain, voxel chunks) and the
/// <see cref="IMapDataProviderRouter"/> that resolves them by binding kind (<see cref="MapDataSourceBinding.Kind"/>).
/// Hosts register concrete providers as singletons implementing the interfaces.
/// </summary>
public static class MapDataServiceCollectionExtensions
{
    public static IServiceCollection AddMapDataProviderRouting(this IServiceCollection services)
    {
        services.TryAddSingleton<IMapDataProviderRouter, MapDataProviderRouter>();
        services.TryAddSingleton<IVectorMapIntelligenceService, NoOpVectorMapIntelligenceService>();
        services.TryAddSingleton<IMaterialIntelligenceService, NoOpMaterialIntelligenceService>();
        services.TryAddSingleton<IMapVerificationService, NoOpMapVerificationService>();
        return services;
    }
}
