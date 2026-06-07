namespace Nexo.GameDomain.Mapping;

/// <summary>
/// Operator-facing hints for host-side geometry stages (runtime does not tessellate here).
/// </summary>
public static class MapPipelineGeometryStageHints
{
    /// <summary>Returns a short hint tied to the adaptation plan.</summary>
    public static string ResolveStageDetail(string stage, MapAdaptationPlan plan)
    {
        var profile = plan.EffectiveMapRenderingProfile;
        var geom = plan.GeometryStrategy;

        return stage switch
        {
            "resolve_aesthetic" =>
                $"ok; active aesthetic resolved ({string.Join("; ", plan.Notes)})",

            "resolve_map_profile" =>
                $"ok; effective profile={profile}; geometryStrategy={geom}",

            "voxel_rasterize" =>
                $"host-side: quantize terrain height + stamp OSM/MVT layers into voxel columns per LOD (profile={profile}).",

            "mesh_displace" =>
                $"host-side: displace mesh vertices using sampled terrain height + overlay masks (profile={profile}).",

            "vector_tessellate" =>
                $"host-side: tessellate fetched vectors to engine meshes or decals (profile={profile}; geom={geom}).",

            "rasterize_overlay" =>
                $"host-side: raster-style overlay / stylized projection from vectors (profile={profile}).",

            _ => "Host-side geometry deferred."
        };
    }
}
