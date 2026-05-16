using Nexo.Core.Application.Environments;

namespace Nexo.Core.Application.Environments.Ports;

/// <summary>
/// Provides stored voxel chunk payloads for a configured pyramid (tile DB / object store).
/// Implementations may read local disk, S3, or a self-hosted chunk HTTP API.
/// </summary>
public interface IVoxelChunkDataProvider
{
    string Kind { get; }

    /// <summary>Returns chunk bytes if present; absent chunks should yield not-found or empty per host policy.</summary>
    Task<VoxelChunkDataResult> FetchAsync(
        VoxelChunkKey key,
        MapDataSourceBinding binding,
        MapDataRequestContext context,
        CancellationToken cancellationToken = default);
}

/// <param name="Found">Whether data exists for this key.</param>
/// <param name="Payload">Voxel or compressed chunk bytes when <see cref="Found"/> is true.</param>
/// <param name="ContentType">Optional format hint.</param>
/// <param name="ETag">Optional cache validator from upstream.</param>
public sealed record VoxelChunkDataResult(
    bool Found,
    ReadOnlyMemory<byte> Payload = default,
    string? ContentType = null,
    string? ETag = null);
