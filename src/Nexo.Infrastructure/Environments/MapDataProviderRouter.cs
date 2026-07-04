using Nexo.Core.Application.Environments;
using Nexo.Core.Application.Environments.Ports;

namespace Nexo.Infrastructure.Environments;

/// <summary>
/// Resolves map data providers by <see cref="MapDataSourceBinding.Kind"/> for dependency injection.
/// Register exactly one implementation per distinct kind per provider category.
/// </summary>
public sealed class MapDataProviderRouter : IMapDataProviderRouter
{
    private readonly IReadOnlyDictionary<string, IVectorMapDataProvider> _vector;
    private readonly IReadOnlyDictionary<string, ITerrainMapDataProvider> _terrain;
    private readonly IReadOnlyDictionary<string, IVoxelChunkDataProvider> _voxel;

    /// <summary>Initializes a new map data provider router.</summary>
    public MapDataProviderRouter(
        IEnumerable<IVectorMapDataProvider> vectorProviders,
        IEnumerable<ITerrainMapDataProvider> terrainProviders,
        IEnumerable<IVoxelChunkDataProvider> voxelProviders)
    {
        _vector = ToDictionary(vectorProviders, static p => p.Kind, nameof(IVectorMapDataProvider));
        _terrain = ToDictionary(terrainProviders, static p => p.Kind, nameof(ITerrainMapDataProvider));
        _voxel = ToDictionary(voxelProviders, static p => p.Kind, nameof(IVoxelChunkDataProvider));
    }

    /// <summary>Resolve vector.</summary>
    public IVectorMapDataProvider ResolveVector(MapDataSourceBinding binding) =>
        Resolve(_vector, binding.Kind, "vector map");

    /// <summary>Resolve terrain.</summary>
    public ITerrainMapDataProvider ResolveTerrain(MapDataSourceBinding binding) =>
        Resolve(_terrain, binding.Kind, "terrain");

    /// <summary>Resolve voxel store.</summary>
    public IVoxelChunkDataProvider ResolveVoxelStore(MapDataSourceBinding binding) =>
        Resolve(_voxel, binding.Kind, "voxel chunk");

    private static T Resolve<T>(IReadOnlyDictionary<string, T> map, string kind, string category)
        where T : class
    {
        var key = kind.Trim();
        if (map.TryGetValue(key, out var p))
            return p;
        throw new KeyNotFoundException(
            $"No {category} data provider registered for Kind=\"{kind}\".");
    }

    private static Dictionary<string, T> ToDictionary<T>(
        IEnumerable<T> providers,
        Func<T, string> kindSelector,
        string registrationLabel)
    {
        var d = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in providers)
        {
            var k = kindSelector(p).Trim();
            if (string.IsNullOrEmpty(k))
                throw new InvalidOperationException($"{registrationLabel} registration has empty Kind.");
            if (!d.TryAdd(k, p))
                throw new InvalidOperationException(
                    $"Duplicate {registrationLabel} Kind=\"{k}\". Only one provider per Kind is allowed.");
        }
        return d;
    }
}
