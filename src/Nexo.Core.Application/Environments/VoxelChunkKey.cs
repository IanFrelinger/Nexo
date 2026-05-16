namespace Nexo.Core.Application.Environments;

/// <summary>
/// Identifies a voxel chunk in the streaming pyramid (tier + integer chunk coordinates).
/// </summary>
/// <param name="TierIndex">LOD tier from <c>VoxelLodTier.TierIndex</c> in game manifests.</param>
/// <param name="ChunkX">Chunk index along world X.</param>
/// <param name="ChunkY">Chunk index along world Y.</param>
/// <param name="ChunkZ">Chunk index along world Z.</param>
/// <param name="LayerId">Optional layer key matching <c>VoxelLodTier.LayerId</c>.</param>
public sealed record VoxelChunkKey(
    int TierIndex,
    int ChunkX,
    int ChunkY,
    int ChunkZ,
    string? LayerId = null);
