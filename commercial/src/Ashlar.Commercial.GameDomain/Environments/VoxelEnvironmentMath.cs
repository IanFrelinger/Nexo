namespace Ashlar.Commercial.GameDomain.Environments;
/// <summary>
/// Helpers for deriving density metrics from <see cref="VoxelLodTier"/> descriptions.
/// </summary>
public static class VoxelEnvironmentMath
{
    /// <summary>
    /// Returns approximate voxel count per axis-aligned chunk at the given tier (equivalent to
    /// <c>ChunkEdgeVoxels³</c>).
    /// </summary>
    public static long ApproximateVoxelsPerChunk(VoxelLodTier tier)
    {
        var n = (long)tier.ChunkEdgeVoxels;
        return n * n * n;
    }

    /// <summary>
    /// World-space extent of one chunk edge at this tier (meters).
    /// </summary>
    public static double ChunkExtentMeters(VoxelLodTier tier) =>
        tier.VoxelSizeMeters * tier.ChunkEdgeVoxels;

    /// <summary>
    /// Voxel density along one axis for a unit cube of world space at this tier (voxels per meter).
    /// </summary>
    public static double VoxelsPerMeter(VoxelLodTier tier) =>
        tier.VoxelSizeMeters > 0 ? 1.0 / tier.VoxelSizeMeters : 0;
}
