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

    /// <summary>
    /// Replaces default no-op map intelligence with <see cref="ModelBackedVectorMapIntelligenceService"/>,
    /// <see cref="ModelBackedMaterialIntelligenceService"/>, and <see cref="OsmSharpMapVerificationService"/>.
    /// Call after <see cref="AddMapDataProviderRouting"/> / <c>AddNexo</c> when <see cref="Nexo.Abstractions.IModel"/> is registered.
    /// </summary>
    public static IServiceCollection ReplaceMapIntelligenceWithModelBackedDefaults(this IServiceCollection services)
    {
        services.RemoveAll<IVectorMapIntelligenceService>();
        services.RemoveAll<IMaterialIntelligenceService>();
        services.RemoveAll<IMapVerificationService>();
        services.AddSingleton<IVectorMapIntelligenceService, ModelBackedVectorMapIntelligenceService>();
        services.AddSingleton<IMaterialIntelligenceService, ModelBackedMaterialIntelligenceService>();
        services.AddSingleton<IMapVerificationService, OsmSharpMapVerificationService>();
        return services;
    }

    /// <summary>
    /// Swap only verification for <see cref="OsmSharpMapVerificationService"/> (deterministic OSM checks).
    /// </summary>
    public static IServiceCollection ReplaceMapVerificationWithOsmSharp(this IServiceCollection services)
    {
        services.RemoveAll<IMapVerificationService>();
        services.AddSingleton<IMapVerificationService, OsmSharpMapVerificationService>();
        return services;
    }
}
