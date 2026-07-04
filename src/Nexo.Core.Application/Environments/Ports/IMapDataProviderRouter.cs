using Nexo.Core.Application.Environments;

namespace Nexo.Core.Application.Environments.Ports;

/// <summary>
/// Optional façade that bundles vector, terrain, and voxel providers behind manifest-driven
/// <see cref="MapDataSourceBinding"/> selections. Hosts may omit this and inject providers directly.
/// </summary>
public interface IMapDataProviderRouter
{
    /// <summary>Resolves a vector map provider for the given binding.</summary>
    /// <param name="binding">Data source connection with kind discriminator.</param>
    IVectorMapDataProvider ResolveVector(MapDataSourceBinding binding);

    /// <summary>Resolves a terrain map provider for the given binding.</summary>
    /// <param name="binding">Data source connection with kind discriminator.</param>
    ITerrainMapDataProvider ResolveTerrain(MapDataSourceBinding binding);

    /// <summary>Resolves a voxel chunk store provider for the given binding.</summary>
    /// <param name="binding">Data source connection with kind discriminator.</param>
    IVoxelChunkDataProvider ResolveVoxelStore(MapDataSourceBinding binding);
}
