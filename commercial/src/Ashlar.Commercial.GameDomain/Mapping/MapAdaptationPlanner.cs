using System.Linq;
using Ashlar.Commercial.GameDomain.Aesthetics;
using Ashlar.Commercial.GameDomain.Scoping;
using Ashlar.Commercial.GameDomain.Session;

namespace Ashlar.Commercial.GameDomain.Mapping;
/// <summary>
/// Derives <see cref="MapAdaptationPlan"/> from session aesthetics and built-in catalog entries.
/// </summary>
public static class MapAdaptationPlanner
{
    /// <summary>
    /// Resolves the active <see cref="AestheticPack"/> (same rules as <see cref="Plan"/>).
    /// </summary>
    public static AestheticPack GetActivePack(SessionState session, IReadOnlyList<AestheticPack> builtInCatalog)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(builtInCatalog);
        var aestheticId = ResolveActiveAestheticId(session);
        return ResolvePack(session, builtInCatalog, aestheticId);
    }

    /// <summary>
    /// Resolves the active aesthetic pack using server-scoped <c>aesthetic</c> setting when present,
    /// otherwise the first pack in <paramref name="session"/>.<see cref="SessionState.AestheticPacks"/>.
    /// </summary>
    public static MapAdaptationPlan Plan(SessionState session, IReadOnlyList<AestheticPack> builtInCatalog)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(builtInCatalog);

        var pack = GetActivePack(session, builtInCatalog);

        var profile = string.IsNullOrWhiteSpace(pack.MapRenderingProfile)
            ? MapRenderingProfiles.Auto
            : pack.MapRenderingProfile.Trim();

        var effective = profile.Equals(MapRenderingProfiles.Auto, StringComparison.OrdinalIgnoreCase)
            ? InferProfileFromGeometry(pack.GeometryStrategy)
            : profile;

        var stages = BuildStages(effective, pack.GeometryStrategy);
        var notes = new List<string>
        {
            $"Active aesthetic: {pack.Id} ({pack.Name})",
            $"Geometry strategy: {pack.GeometryStrategy}"
        };

        return new MapAdaptationPlan(effective, pack.GeometryStrategy, stages, notes);
    }

    private static string? ResolveActiveAestheticId(SessionState session)
    {
        var server = session.ScopedSettings
            .LastOrDefault(s =>
                s.SettingId == "aesthetic" &&
                s.Scope.Type == SettingScopeType.Server);

        return server?.Value?.ToString();
    }

    private static AestheticPack ResolvePack(
        SessionState session,
        IReadOnlyList<AestheticPack> builtInCatalog,
        string? aestheticId)
    {
        if (!string.IsNullOrWhiteSpace(aestheticId))
        {
            var fromSession = session.AestheticPacks.FirstOrDefault(p =>
                p.Id.Equals(aestheticId.Trim(), StringComparison.OrdinalIgnoreCase));
            if (fromSession is not null)
                return fromSession;

            var fromBuiltIn = builtInCatalog.FirstOrDefault(p =>
                p.Id.Equals(aestheticId.Trim(), StringComparison.OrdinalIgnoreCase));
            if (fromBuiltIn is not null)
                return fromBuiltIn;
        }

        return session.AestheticPacks.FirstOrDefault()
               ?? builtInCatalog.FirstOrDefault()
               ?? new AestheticPack { Id = "default", Name = "Default", GeometryStrategy = "low_poly" };
    }

    private static string InferProfileFromGeometry(string geometry)
    {
        return geometry.ToLowerInvariant() switch
        {
            "voxel" => MapRenderingProfiles.VoxelGrid,
            "pbr" => MapRenderingProfiles.HeightfieldMesh,
            "low_poly" => MapRenderingProfiles.FlatShadedPolys,
            "pixel_art" => MapRenderingProfiles.OrthographicTile,
            "wireframe" or "sketch" => MapRenderingProfiles.VectorOverlay,
            _ => MapRenderingProfiles.HeightfieldMesh
        };
    }

    private static IReadOnlyList<string> BuildStages(string profile, string geometry)
    {
        var list = new List<string> { "resolve_aesthetic", "resolve_map_profile" };

        if (profile is MapRenderingProfiles.VoxelGrid or MapRenderingProfiles.HeightfieldMesh)
        {
            list.Add("fetch_terrain");
            list.Add("fetch_vector");
            list.Add(geometry.Equals("voxel", StringComparison.OrdinalIgnoreCase)
                ? "voxel_rasterize"
                : "mesh_displace");
        }
        else if (profile is MapRenderingProfiles.VectorOverlay or MapRenderingProfiles.FlatShadedPolys)
        {
            list.Add("fetch_vector");
            list.Add("vector_tessellate");
        }
        else
        {
            list.Add("fetch_vector");
            list.Add("rasterize_overlay");
        }

        list.Add("emit_host_manifest");
        return list;
    }
}
