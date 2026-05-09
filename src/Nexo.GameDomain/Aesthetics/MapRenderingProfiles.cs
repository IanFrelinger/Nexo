namespace Nexo.GameDomain.Aesthetics;

/// <summary>
/// Canonical identifiers for how geographic data should be processed for rendering
/// (independent of the host engine).
/// </summary>
public static class MapRenderingProfiles
{
    public const string Auto = "auto";
    public const string VoxelGrid = "voxel_grid";
    public const string HeightfieldMesh = "heightfield_mesh";
    public const string FlatShadedPolys = "flat_shaded_polys";
    public const string BillboardFeatures = "billboard_features";
    public const string VectorOverlay = "vector_overlay";
    public const string OrthographicTile = "orthographic_tile";
}
