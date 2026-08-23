namespace Ashlar.Core.Application.Environments;

/// <summary>Conventional <see cref="MapVerificationIssue.Category"/> values.</summary>
public static class MapVerificationCategories
{
    /// <summary>Hydrology and water body topology checks.</summary>
    public const string Water = "water";

    /// <summary>Road network connectivity and routing checks.</summary>
    public const string Road = "road";

    /// <summary>Building footprint and placement checks.</summary>
    public const string Building = "building";

    /// <summary>Elevation and terrain alignment checks.</summary>
    public const string Terrain = "terrain";

    /// <summary>General graph topology and tiling consistency checks.</summary>
    public const string Topology = "topology";

    /// <summary>Voxel chunk boundary and pyramid alignment checks.</summary>
    public const string VoxelChunk = "voxel_chunk";
}
