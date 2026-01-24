using Nexo.GeoTerrain;
using Nexo.GeoVector.Geometry;
using Nexo.GeoVector.Models;
using Nexo.GeoVector.Values;
using Nexo.Orchestration.GeoVector.Ports;

namespace Nexo.Adapters.GeoVector.Providers;

/// <summary>
/// Offline-friendly synthetic provider that returns deterministic toy features.
/// Useful for demos, tests, and air-gapped scenarios.
/// </summary>
public sealed class EchoVectorProvider : IVectorProvider
{
    public Task<GeoFeatureSet> GetFeaturesAsync(GeoBounds bounds, FeatureKind kind, CancellationToken cancellationToken = default)
    {
        bounds.Validate();
        kind ??= FeatureKind.Building;

        // Emit deterministic toy features near the bounds center.
        var centerLat = (bounds.MinLatitude.Degrees + bounds.MaxLatitude.Degrees) * 0.5;
        var centerLon = (bounds.MinLongitude.Degrees + bounds.MaxLongitude.Degrees) * 0.5;

        var d = 0.00005; // ~5-6m at equator
        var ring = new[]
        {
            new GeoPoint { Latitude = new Latitude(centerLat - d), Longitude = new Longitude(centerLon - d) },
            new GeoPoint { Latitude = new Latitude(centerLat - d), Longitude = new Longitude(centerLon + d) },
            new GeoPoint { Latitude = new Latitude(centerLat + d), Longitude = new Longitude(centerLon + d) },
            new GeoPoint { Latitude = new Latitude(centerLat + d), Longitude = new Longitude(centerLon - d) },
            new GeoPoint { Latitude = new Latitude(centerLat - d), Longitude = new Longitude(centerLon - d) }
        };

        if (kind.Equals(FeatureKind.Building))
        {
            var f = new GeoFeature(
                id: "echo-building-1",
                kind: FeatureKind.Building,
                geometry: new GeoPolygon(ring),
                properties: new Dictionary<string, object>
                {
                    ["height_m"] = 12f,
                    ["provider"] = "echo"
                });
            return Task.FromResult(new GeoFeatureSet(new[] { f }));
        }

        if (kind.Equals(FeatureKind.Road))
        {
            var line = new[]
            {
                new GeoPoint { Latitude = new Latitude(centerLat - d), Longitude = new Longitude(centerLon - d) },
                new GeoPoint { Latitude = new Latitude(centerLat + d), Longitude = new Longitude(centerLon + d) }
            };
            var f = new GeoFeature(
                id: "echo-road-1",
                kind: FeatureKind.Road,
                geometry: new GeoPolyline(line),
                properties: new Dictionary<string, object>
                {
                    ["class"] = "residential",
                    ["provider"] = "echo"
                });
            return Task.FromResult(new GeoFeatureSet(new[] { f }));
        }

        if (kind.Equals(FeatureKind.Vegetation))
        {
            var f = new GeoFeature(
                id: "echo-park-1",
                kind: FeatureKind.Vegetation,
                geometry: new GeoPolygon(ring),
                properties: new Dictionary<string, object>
                {
                    ["landuse"] = "park",
                    ["provider"] = "echo"
                });
            return Task.FromResult(new GeoFeatureSet(new[] { f }));
        }

        if (kind.Equals(FeatureKind.Water))
        {
            var f = new GeoFeature(
                id: "echo-water-1",
                kind: FeatureKind.Water,
                geometry: new GeoPolygon(ring),
                properties: new Dictionary<string, object>
                {
                    ["natural"] = "water",
                    ["provider"] = "echo"
                });
            return Task.FromResult(new GeoFeatureSet(new[] { f }));
        }

        // Default: empty set for unsupported kinds.
        return Task.FromResult(new GeoFeatureSet(Array.Empty<GeoFeature>()));
    }
}

