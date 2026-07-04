using Nexo.Commercial.GameDomain.Aesthetics;

namespace Nexo.Commercial.GameDomain.Mapping;
/// <summary>
/// Derives a web-tile zoom pyramid from aesthetic <see cref="LodLevel"/> entries and a **finest** map zoom.
/// <para>
/// Convention: <see cref="LodLevel.Level"/> 0 is closest to the camera (finest). Each +1 level steps one zoom
/// level coarser: <c>Zoom = max(0, finestZoom - LodLevel.Level)</c>. Hosts use this to request tiles, stream
/// assets, and swap geometry by distance.
/// </para>
/// </summary>
public static class MapLodPyramidPlanner
{
    /// <summary>Default reference zoom if the API omits <c>finestZoom</c> (Mapbox-style street detail).</summary>
    public const int DefaultFinestZoom = 14;

    /// <summary>
    /// Builds ordered tiers (by <see cref="LodLevel.Level"/> ascending). When no LODs exist, returns a single tier.
    /// </summary>
    /// <param name="lodLevels">From the active aesthetic pack.</param>
    /// <param name="finestZoom">Max-detail tile zoom (0–22), e.g. 14 for neighborhood scale.</param>
    public static IReadOnlyList<MapTilePyramidTier> Build(
        IReadOnlyList<LodLevel> lodLevels,
        int finestZoom)
    {
        if (finestZoom is < 0 or > 22)
            throw new ArgumentOutOfRangeException(nameof(finestZoom), finestZoom, "finestZoom must be in [0,22].");

        if (lodLevels.Count == 0)
        {
            return
            [
                new MapTilePyramidTier(0, 1.0, finestZoom, finestZoom, 0, 1.0)
            ];
        }

        var list = new List<MapTilePyramidTier>();
        foreach (var lod in lodLevels.OrderBy(l => l.Level))
        {
            var z = Math.Max(0, finestZoom - lod.Level);
            var delta = finestZoom - z;
            var areaMult = Math.Pow(4, delta);
            list.Add(new MapTilePyramidTier(lod.Level, lod.DetailFactor, z, finestZoom, delta, areaMult));
        }

        return list;
    }
}
