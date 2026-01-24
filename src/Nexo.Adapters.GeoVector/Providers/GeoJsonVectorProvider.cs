using Microsoft.Extensions.Logging;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nexo.GeoTerrain;
using Nexo.GeoVector.Geometry;
using Nexo.GeoVector.Models;
using Nexo.GeoVector.Values;
using Nexo.Orchestration.GeoVector.Ports;

namespace Nexo.Adapters.GeoVector.Providers;

/// <summary>
/// Loads vector features from GeoJSON files (local disk, air-gapped friendly).
/// Supports FeatureCollection and single Feature/Geometry objects.
/// </summary>
public sealed class GeoJsonVectorProvider : IVectorProvider
{
    private readonly string _filePath;
    private readonly ILogger<GeoJsonVectorProvider>? _logger;

    public GeoJsonVectorProvider(string filePath, ILogger<GeoJsonVectorProvider>? logger = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("filePath is required.", nameof(filePath));
        _filePath = filePath;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<GeoJsonVectorProvider>.Instance;
    }

    public Task<GeoFeatureSet> GetFeaturesAsync(GeoBounds bounds, FeatureKind kind, CancellationToken cancellationToken = default)
    {
        bounds.Validate();
        kind ??= FeatureKind.Building;

        if (!File.Exists(_filePath))
            throw new FileNotFoundException($"GeoJSON file not found: {_filePath}");

        var json = File.ReadAllText(_filePath);
        var features = ParseGeoJson(json, bounds, kind, cancellationToken);

        _logger?.LogInformation("GeoJSON provider returned {Count} feature(s) for kind={Kind} from {Path}", 
            features.Count, kind.Value, _filePath);

        return Task.FromResult(new GeoFeatureSet(features));
    }

    private IReadOnlyList<GeoFeature> ParseGeoJson(string json, GeoBounds bounds, FeatureKind kind, CancellationToken ct)
    {
        var results = new List<GeoFeature>();

        try
        {
            var root = JObject.Parse(json);
            var type = root["type"]?.ToString();

            if (type == "FeatureCollection")
            {
                var features = root["features"] as JArray;
                if (features != null)
                {
                    foreach (var featureToken in features)
                    {
                        ct.ThrowIfCancellationRequested();
                        var feature = ParseFeature(featureToken as JObject, bounds, kind);
                        if (feature != null)
                        {
                            results.Add(feature);
                        }
                    }
                }
            }
            else if (type == "Feature")
            {
                var feature = ParseFeature(root, bounds, kind);
                if (feature != null)
                {
                    results.Add(feature);
                }
            }
            else
            {
                throw new InvalidOperationException($"Unsupported GeoJSON type: {type}");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to parse GeoJSON from {Path}", _filePath);
            throw new InvalidOperationException($"Invalid GeoJSON format in {_filePath}", ex);
        }

        return results;
    }

    private GeoFeature? ParseFeature(JObject? featureObj, GeoBounds bounds, FeatureKind kind)
    {
        if (featureObj == null) return null;

        var geometryObj = featureObj["geometry"] as JObject;
        if (geometryObj == null) return null;

        var geomType = geometryObj["type"]?.ToString();
        IGeoGeometry? geoGeom = null;

        if (geomType == "Polygon")
        {
            geoGeom = ParsePolygon(geometryObj);
        }
        else if (geomType == "MultiPolygon")
        {
            // Take first polygon for now
            var coords = geometryObj["coordinates"] as JArray;
            if (coords != null && coords.Count > 0)
            {
                var firstRing = coords[0] as JArray;
                if (firstRing != null)
                {
                    geoGeom = ParsePolygonFromCoords(firstRing);
                }
            }
        }
        else if (geomType == "LineString")
        {
            geoGeom = ParseLineString(geometryObj);
        }
        else if (geomType == "MultiLineString")
        {
            var coords = geometryObj["coordinates"] as JArray;
            if (coords != null && coords.Count > 0)
            {
                var firstLine = coords[0] as JArray;
                if (firstLine != null)
                {
                    geoGeom = ParseLineStringFromCoords(firstLine);
                }
            }
        }

        if (geoGeom == null) return null;

        // Check bounds intersection
        if (!GeometryIntersectsBounds(geoGeom, bounds)) return null;

        // Extract properties
        var props = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        var properties = featureObj["properties"] as JObject;
        if (properties != null)
        {
            foreach (var prop in properties.Properties())
            {
                props[prop.Name] = prop.Value?.ToString() ?? "";
            }
        }
        props["provider"] = "geojson";
        props["source"] = Path.GetFileName(_filePath);

        var id = featureObj["id"]?.ToString();
        if (string.IsNullOrWhiteSpace(id) && props.ContainsKey("id"))
        {
            id = props["id"]?.ToString();
        }
        if (string.IsNullOrWhiteSpace(id)) id = "geojson-feature";

        return new GeoFeature(id, kind, geoGeom, props);
    }

    private static bool GeometryIntersectsBounds(IGeoGeometry geom, GeoBounds bounds)
    {
        // Quick check: if any point in geometry is within bounds
        if (geom is GeoPolygon poly)
        {
            return poly.Points.Any(bounds.Contains);
        }
        if (geom is GeoPolyline line)
        {
            return line.Points.Any(bounds.Contains);
        }
        return true; // Unknown geometry type, include it
    }

    private GeoPolygon? ParsePolygon(JObject geometryObj)
    {
        var coords = geometryObj["coordinates"] as JArray;
        if (coords == null || coords.Count == 0) return null;

        var exteriorRing = coords[0] as JArray;
        if (exteriorRing == null) return null;

        return ParsePolygonFromCoords(exteriorRing);
    }

    private static GeoPolygon ParsePolygonFromCoords(JArray ring)
    {
        var points = new List<GeoPoint>();
        foreach (var coord in ring)
        {
            var coordArray = coord as JArray;
            if (coordArray == null || coordArray.Count < 2) continue;

            var lon = coordArray[0].Value<double>();
            var lat = coordArray[1].Value<double>();
            points.Add(new GeoPoint
            {
                Longitude = new Longitude(lon),
                Latitude = new Latitude(lat)
            });
        }

        if (points.Count < 3) throw new InvalidOperationException("Polygon must have at least 3 points.");

        // Ensure closed
        if (points[0].Latitude.Degrees != points[^1].Latitude.Degrees ||
            points[0].Longitude.Degrees != points[^1].Longitude.Degrees)
        {
            points.Add(points[0]);
        }

        return new GeoPolygon(points);
    }

    private GeoPolyline? ParseLineString(JObject geometryObj)
    {
        var coords = geometryObj["coordinates"] as JArray;
        if (coords == null) return null;

        return ParseLineStringFromCoords(coords);
    }

    private static GeoPolyline ParseLineStringFromCoords(JArray coords)
    {
        var points = new List<GeoPoint>();
        foreach (var coord in coords)
        {
            var coordArray = coord as JArray;
            if (coordArray == null || coordArray.Count < 2) continue;

            var lon = coordArray[0].Value<double>();
            var lat = coordArray[1].Value<double>();
            points.Add(new GeoPoint
            {
                Longitude = new Longitude(lon),
                Latitude = new Latitude(lat)
            });
        }

        if (points.Count < 2) throw new InvalidOperationException("LineString must have at least 2 points.");

        return new GeoPolyline(points);
    }

    private static bool GeometryIntersectsBounds(Geometry geom, GeoBounds bounds)
    {
        // Quick bounds check: if geometry's envelope intersects query bounds
        var env = geom.EnvelopeInternal;
        return env.MaxX >= bounds.MinLongitude.Degrees &&
               env.MinX <= bounds.MaxLongitude.Degrees &&
               env.MaxY >= bounds.MinLatitude.Degrees &&
               env.MinY <= bounds.MaxLatitude.Degrees;
    }

}
