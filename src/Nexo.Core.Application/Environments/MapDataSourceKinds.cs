namespace Nexo.Core.Application.Environments;

/// <summary>Conventional <see cref="MapDataSourceBinding.Kind"/> values.</summary>
public static class MapDataSourceKinds
{
    /// <summary>HTTP or REST map data provider.</summary>
    public const string Http = "http";

    /// <summary>Local filesystem map data provider.</summary>
    public const string File = "file";

    /// <summary>OpenStreetMap Overpass API provider.</summary>
    public const string Overpass = "overpass";

    /// <summary>Raster terrain tile provider.</summary>
    public const string TerrainTiles = "terrain_tiles";

    /// <summary>Voxel chunk object store or tile database.</summary>
    public const string VoxelStore = "voxel_store";

    /// <summary>In-memory provider for tests and embedded scenarios.</summary>
    public const string InMemory = "in_memory";
}
