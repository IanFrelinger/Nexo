using Microsoft.Extensions.Logging;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Nexo.GeoTerrain;
using Nexo.GeoVector.Geometry;
using Nexo.GeoVector.Models;
using Nexo.GeoVector.Values;
using Nexo.Orchestration.GeoVector.Ports;

namespace Nexo.Adapters.GeoVector.Providers;

/// <summary>
/// Loads vector features from Shapefile format (local disk, air-gapped friendly).
/// Supports .shp files with associated .dbf and .shx files.
/// </summary>
public sealed class ShapefileVectorProvider : IVectorProvider
{
    private readonly string _shapefilePath;
    private readonly ILogger<ShapefileVectorProvider>? _logger;

    public ShapefileVectorProvider(string shapefilePath, ILogger<ShapefileVectorProvider>? logger = null)
    {
        if (string.IsNullOrWhiteSpace(shapefilePath))
            throw new ArgumentException("shapefilePath is required.", nameof(shapefilePath));
        _shapefilePath = shapefilePath;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ShapefileVectorProvider>.Instance;
    }

    public Task<GeoFeatureSet> GetFeaturesAsync(GeoBounds bounds, FeatureKind kind, CancellationToken cancellationToken = default)
    {
        bounds.Validate();
        kind ??= FeatureKind.Building;

        if (!File.Exists(_shapefilePath))
            throw new FileNotFoundException($"Shapefile not found: {_shapefilePath}");

        var features = ReadShapefile(_shapefilePath, bounds, kind, cancellationToken);

        _logger?.LogInformation("Shapefile provider returned {Count} feature(s) for kind={Kind} from {Path}",
            features.Count, kind.Value, _shapefilePath);

        return Task.FromResult(new GeoFeatureSet(features));
    }

    private IReadOnlyList<GeoFeature> ReadShapefile(string path, GeoBounds bounds, FeatureKind kind, CancellationToken ct)
    {
        var results = new List<GeoFeature>();

        try
        {
            using var shapefileReader = new ShapefileDataReader(path, new GeometryFactory());
            var header = shapefileReader.DbaseHeader;
            var fieldCount = header?.NumFields ?? 0;

            var fieldNames = new List<string>();
            if (header != null)
            {
                for (var i = 0; i < fieldCount; i++)
                {
                    fieldNames.Add(header.Fields[i].Name);
                }
            }

            while (shapefileReader.Read() && !ct.IsCancellationRequested)
            {
                var geometry = shapefileReader.Geometry;
                if (geometry == null) continue;

                // Convert NetTopologySuite geometry to our geometry types
                IGeoGeometry? geoGeom = null;
                if (geometry is Polygon polygon)
                {
                    geoGeom = ConvertPolygon(polygon);
                }
                else if (geometry is LineString lineString)
                {
                    geoGeom = ConvertLineString(lineString);
                }
                else if (geometry is MultiPolygon multiPolygon && multiPolygon.NumGeometries > 0)
                {
                    // Take first polygon
                    geoGeom = ConvertPolygon((Polygon)multiPolygon.GetGeometryN(0));
                }
                else if (geometry is MultiLineString multiLineString && multiLineString.NumGeometries > 0)
                {
                    // Take first line
                    geoGeom = ConvertLineString((LineString)multiLineString.GetGeometryN(0));
                }

                if (geoGeom == null) continue;

                // Check bounds intersection
                if (!GeometryIntersectsBounds(geoGeom, bounds)) continue;

                // Extract attributes
                var props = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < fieldCount; i++)
                {
                    var fieldName = fieldNames[i];
                    var value = shapefileReader.GetValue(i);
                    props[fieldName] = value?.ToString() ?? "";
                }
                props["provider"] = "shapefile";
                props["source"] = Path.GetFileName(path);

                // Generate ID from FID or row number
                var id = props.ContainsKey("FID") ? props["FID"]?.ToString() : null;
                if (string.IsNullOrWhiteSpace(id))
                {
                    id = $"shapefile-{results.Count}";
                }

                results.Add(new GeoFeature(id, kind, geoGeom, props));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to read Shapefile from {Path}", path);
            throw new InvalidOperationException($"Invalid Shapefile format in {path}", ex);
        }

        return results;
    }

    private static bool GeometryIntersectsBounds(IGeoGeometry geom, GeoBounds bounds)
    {
        if (geom is GeoPolygon poly)
        {
            return poly.Points.Any(bounds.Contains);
        }
        if (geom is GeoPolyline line)
        {
            return line.Points.Any(bounds.Contains);
        }
        return true;
    }

    private static GeoPolygon ConvertPolygon(Polygon polygon)
    {
        var exteriorRing = polygon.ExteriorRing;
        var points = new List<GeoPoint>();

        foreach (Coordinate coord in exteriorRing.Coordinates)
        {
            points.Add(new GeoPoint
            {
                Longitude = new Longitude(coord.X),
                Latitude = new Latitude(coord.Y)
            });
        }

        if (points.Count < 3)
            throw new InvalidOperationException("Polygon must have at least 3 points.");

        return new GeoPolygon(points);
    }

    private static GeoPolyline ConvertLineString(LineString lineString)
    {
        var points = new List<GeoPoint>();

        foreach (Coordinate coord in lineString.Coordinates)
        {
            points.Add(new GeoPoint
            {
                Longitude = new Longitude(coord.X),
                Latitude = new Latitude(coord.Y)
            });
        }

        if (points.Count < 2)
            throw new InvalidOperationException("LineString must have at least 2 points.");

        return new GeoPolyline(points);
    }
}
