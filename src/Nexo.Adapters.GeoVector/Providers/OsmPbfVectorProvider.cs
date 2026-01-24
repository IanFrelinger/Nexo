using Microsoft.Extensions.Logging;
using Nexo.GeoTerrain;
using Nexo.GeoVector.Geometry;
using Nexo.GeoVector.Models;
using Nexo.GeoVector.Values;
using Nexo.Orchestration.GeoVector.Ports;
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Tags;

namespace Nexo.Adapters.GeoVector.Providers;

/// <summary>
/// Offline-friendly provider that reads an OSM PBF extract and returns features for a GeoBounds.
/// v0 supports closed building/landuse/water ways (polygons) and highway ways (polylines).
/// Relations/multipolygons are ignored for now.
/// </summary>
public sealed class OsmPbfVectorProvider : IVectorProvider
{
    private readonly string _pbfPath;
    private readonly ILogger<OsmPbfVectorProvider> _logger;

    public OsmPbfVectorProvider(string pbfPath, ILogger<OsmPbfVectorProvider>? logger = null)
    {
        if (string.IsNullOrWhiteSpace(pbfPath))
            throw new ArgumentException("pbfPath is required.", nameof(pbfPath));
        _pbfPath = pbfPath;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<OsmPbfVectorProvider>.Instance;
    }

    public Task<GeoFeatureSet> GetFeaturesAsync(GeoBounds bounds, FeatureKind kind, CancellationToken cancellationToken = default)
    {
        bounds.Validate();
        kind ??= FeatureKind.Building;

        // Keep this sync for now; PBF streaming is CPU/IO bound and usually local disk.
        var features = ReadFeatures(bounds, kind, cancellationToken);
        return Task.FromResult(new GeoFeatureSet(features));
    }

    private IReadOnlyList<GeoFeature> ReadFeatures(GeoBounds bounds, FeatureKind kind, CancellationToken ct)
    {
        if (!File.Exists(_pbfPath))
            throw new FileNotFoundException("OSM PBF file not found.", _pbfPath);

        // OSM objects are usually ordered: nodes -> ways -> relations.
        var nodeIndex = new Dictionary<long, (double lat, double lon)>();
        var results = new List<GeoFeature>(capacity: 1024);

        using var fs = File.OpenRead(_pbfPath);
        var source = new PBFOsmStreamSource(fs);

        foreach (var obj in source)
        {
            ct.ThrowIfCancellationRequested();
            if (obj == null) continue;

            if (obj.Type == OsmGeoType.Node)
            {
                var n = (Node)obj;
                if (!n.Id.HasValue || !n.Latitude.HasValue || !n.Longitude.HasValue) continue;
                nodeIndex[n.Id.Value] = (n.Latitude.Value, n.Longitude.Value);
                continue;
            }

            if (obj.Type != OsmGeoType.Way) continue;
            var way = (Way)obj;
            if (!way.Id.HasValue) continue;

            var tags = way.Tags;
            if (tags == null || tags.Count == 0) continue;

            if (!MatchesKind(tags, kind)) continue;

            var nodes = way.Nodes;
            if (nodes == null || nodes.Length < 2) continue;

            List<GeoPoint>? pts = new(nodes.Length);
            for (var i = 0; i < nodes.Length; i++)
            {
                if (!nodeIndex.TryGetValue(nodes[i], out var ll))
                {
                    pts = null;
                    break;
                }
                pts.Add(new GeoPoint { Latitude = new Latitude(ll.lat), Longitude = new Longitude(ll.lon) });
            }

            if (pts == null || pts.Count < 2) continue;

            // Filter fast: require at least one vertex within bounds.
            if (!pts.Any(bounds.Contains)) continue;

            var isClosed = nodes.Length >= 4 && nodes[0] == nodes[^1];

            IGeoGeometry? geom = null;
            if (isClosed && pts.Count >= 4 && (kind.Equals(FeatureKind.Building) || kind.Equals(FeatureKind.Vegetation) || kind.Equals(FeatureKind.Water)))
            {
                geom = new GeoPolygon(pts);
            }
            else if (kind.Equals(FeatureKind.Road))
            {
                geom = new GeoPolyline(pts);
            }

            if (geom == null) continue;

            var props = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in tags)
            {
                props[kv.Key] = kv.Value ?? "";
            }
            props["provider"] = "osm";
            props["source"] = Path.GetFileName(_pbfPath);

            results.Add(new GeoFeature(
                id: $"osm-way-{way.Id.Value}",
                kind: kind,
                geometry: geom,
                properties: props));
        }

        _logger.LogInformation("OSM PBF provider returned {Count} feature(s) for kind={Kind}", results.Count, kind.Value);
        return results;
    }

    private static bool MatchesKind(TagsCollectionBase tags, FeatureKind kind)
    {
        if (kind.Equals(FeatureKind.Building))
        {
            return tags.ContainsKey("building");
        }

        if (kind.Equals(FeatureKind.Road))
        {
            return tags.ContainsKey("highway");
        }

        if (kind.Equals(FeatureKind.Water))
        {
            if (tags.TryGetValue("natural", out var nat) && string.Equals(nat, "water", StringComparison.OrdinalIgnoreCase))
                return true;
            return tags.ContainsKey("waterway");
        }

        if (kind.Equals(FeatureKind.Vegetation))
        {
            if (tags.TryGetValue("natural", out var nat) && string.Equals(nat, "wood", StringComparison.OrdinalIgnoreCase))
                return true;
            if (tags.TryGetValue("landuse", out var landuse))
            {
                return string.Equals(landuse, "forest", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(landuse, "grass", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(landuse, "meadow", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(landuse, "park", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        return false;
    }
}

