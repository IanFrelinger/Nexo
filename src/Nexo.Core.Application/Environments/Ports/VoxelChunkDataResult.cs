using Nexo.Core.Application.Environments;

namespace Nexo.Core.Application.Environments.Ports;

/// <summary>
/// Chunk fetch result from a <see cref="IVoxelChunkDataProvider"/>.
/// </summary>
/// <param name="Found">Whether data exists for this key.</param>
/// <param name="Payload">Voxel or compressed chunk bytes when <see cref="Found"/> is true.</param>
/// <param name="ContentType">Optional format hint.</param>
/// <param name="ETag">Optional cache validator from upstream.</param>
public sealed record VoxelChunkDataResult(
    bool Found,
    ReadOnlyMemory<byte> Payload = default,
    string? ContentType = null,
    string? ETag = null);
