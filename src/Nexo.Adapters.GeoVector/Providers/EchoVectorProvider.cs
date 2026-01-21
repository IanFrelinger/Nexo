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

        // Emit a single square "building" near the bounds center.
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
}

