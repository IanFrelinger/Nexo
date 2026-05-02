using Nexo.Core.Application.Environments;

namespace Nexo.Core.Application.Environments.Ports;

/// <summary>
/// Optional façade that bundles vector, terrain, and voxel providers behind manifest-driven
/// <see cref="MapDataSourceBinding"/> selections. Hosts may omit this and inject providers directly.
/// </summary>
public interface IMapDataProviderRouter
{
    IVectorMapDataProvider ResolveVector(MapDataSourceBinding binding);
    ITerrainMapDataProvider ResolveTerrain(MapDataSourceBinding binding);
    IVoxelChunkDataProvider ResolveVoxelStore(MapDataSourceBinding binding);
}
