using Nexo.Core.Application.Environments;

namespace Nexo.Core.Application.Environments.Ports;

/// <summary>
/// Provides stored voxel chunk payloads for a configured pyramid (tile DB / object store).
/// Implementations may read local disk, S3, or a self-hosted chunk HTTP API.
/// </summary>
public interface IVoxelChunkDataProvider
{
    /// <summary>Discriminator matching <see cref="MapDataSourceBinding.Kind"/> this implementation handles.</summary>
    string Kind { get; }

    /// <summary>Returns chunk bytes if present; absent chunks should yield not-found or empty per host policy.</summary>
    /// <param name="key">Voxel chunk coordinates and tier.</param>
    /// <param name="binding">Data source connection details.</param>
    /// <param name="context">Cross-cutting request context.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<VoxelChunkDataResult> FetchAsync(
        VoxelChunkKey key,
        MapDataSourceBinding binding,
        MapDataRequestContext context,
        CancellationToken cancellationToken = default);
}
