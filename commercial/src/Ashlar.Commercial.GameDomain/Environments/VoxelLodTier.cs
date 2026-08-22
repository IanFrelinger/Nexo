namespace Ashlar.Commercial.GameDomain.Environments;
/// <summary>
/// One composable level in a voxel environment pyramid. Analogous to a zoom level in a tiled map
/// database: each tier defines spatial resolution (voxel size), chunk granularity, and optional
/// asset routing so streaming loaders can fetch the correct layer.
/// </summary>
/// <param name="TierIndex">
/// Pyramid index; <c>0</c> is finest resolution (smallest <see cref="VoxelSizeMeters"/>).
/// Larger indices are coarser summaries used farther from the camera or for previews.
/// </param>
/// <param name="DetailFactor">
/// Normalised detail in <c>[0, 1]</c>, aligned with <see cref="Ashlar.Commercial.GameDomain.Aesthetics.LodLevel.DetailFactor"/>.
/// For voxels: typically <c>1</c> at tier 0 and decreasing for coarser tiers.
/// </param>
/// <param name="VoxelSizeMeters">
/// World-space edge length of one voxel at this tier (meters). Finer tiers use smaller values.
/// </param>
/// <param name="ChunkEdgeVoxels">
/// Number of voxels along one axis of a spatial chunk at this tier.
/// Approximate voxel count per chunk is <c>ChunkEdgeVoxels³</c> (see <see cref="VoxelEnvironmentMath"/>).
/// </param>
/// <param name="LayerId">
/// Stable identifier for this tier in on-disk or remote tile stores (e.g. folder or cache key).
/// </param>
/// <param name="AssetPathTemplate">
/// Optional relative path pattern for chunk payloads, e.g. <c>{layer}/{cx}/{cy}/{cz}.vox</c>.
/// Hosts resolve placeholders; the manifest stays engine-agnostic.
/// </param>
public sealed record VoxelLodTier(
    int TierIndex = 0,
    double DetailFactor = 1.0,
    double VoxelSizeMeters = 1.0,
    int ChunkEdgeVoxels = 32,
    string? LayerId = null,
    string? AssetPathTemplate = null);
