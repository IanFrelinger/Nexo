namespace Nexo.Commercial.GameDomain.Aesthetics;
/// <summary>
/// Identifiers for how real-world map data is rasterised or meshed for play,
/// independent of <see cref="AestheticPack.GeometryStrategy"/> (which targets authored scene content).
/// Hosts pick shaders, mesh builders, and LOD schemes using this profile together with the aesthetic pack.
/// </summary>
public static class MapRenderingProfiles
{
    /// <summary>Host derives an appropriate map pipeline from <see cref="AestheticPack.GeometryStrategy"/> and tags.</summary>
    public const string Auto = "auto";

    /// <summary>Voxel column / tile pyramid matching composable LOD tiers (classic block-world map).</summary>
    public const string VoxelGrid = "voxel_grid";

    /// <summary>DEM or heightfield-driven terrain mesh with draped features.</summary>
    public const string HeightfieldMesh = "heightfield_mesh";

    /// <summary>Extruded or simplified polygon meshes for buildings and roads (low-poly city).</summary>
    public const string FlatShadedPolys = "flat_shaded_polys";

    /// <summary>Sprites, cards, or impostors for trees and landmark features.</summary>
    public const string BillboardFeatures = "billboard_features";

    /// <summary>GIS / cartographic overlay — strokes and fills over terrain or flat plane.</summary>
    public const string VectorOverlay = "vector_overlay";

    /// <summary>Top-down orthographic tile stamp (pixel-art or strategy-map style).</summary>
    public const string OrthographicTile = "orthographic_tile";
}
