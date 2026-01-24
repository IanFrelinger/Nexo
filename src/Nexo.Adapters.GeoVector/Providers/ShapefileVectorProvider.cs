using Microsoft.Extensions.Logging;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NetTopologySuite.IO.ShapeFile;
using Nexo.GeoTerrain;
using Nexo.GeoVector.Geometry;
using Nexo.GeoVector.Models;
using Nexo.GeoVector.Values;
using Nexo.Orchestration.GeoVector.Ports;

namespace Nexo.Adapters.GeoVector.Providers;

/// <summary>
/// Loads vector features from Shapefile format (local disk, air-gapped friendly).
/// Shapefiles consist of multiple files (.shp, .shx, .dbf, etc.) but only the .shp path is required.
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
            var factory = new GeometryFactory();
            using var reader = new ShapefileDataReader(path, factory);
            
            var dbaseHeader = reader.DbaseHeader;
            var recordNumber = 0;

            while (reader.Read())
            {
                ct.ThrowIfCancellationRequested();
                recordNumber++;

                var geometry = reader.Geometry;
                if (geometry == null) continue;

                // Check bounds intersection
                var env = geometry.EnvelopeInternal;
                if (env.MaxX < bounds.MinLongitude.Degrees ||
                    env.MinX > bounds.MaxLongitude.Degrees ||
                    env.MaxY < bounds.MinLatitude.Degrees ||
                    env.MinY > bounds.MaxLatitude.Degrees)
                {
                    continue; // Feature outside query bounds
                }

                // Convert NTS geometry to GeoVector geometry
                IGeoGeometry? geoGeom = null;
                if (geometry is Polygon poly)
                {
                    geoGeom = ConvertPolygon(poly);
                }
                else if (geometry is MultiPolygon mp)
                {
                    // Take first polygon for now
                    if (mp.NumGeometries > 0 && mp.GetGeometryN(0) is Polygon firstPoly)
                    {
                        geoGeom = ConvertPolygon(firstPoly);
                    }
                }
                else if (geometry is LineString ls)
                {
                    geoGeom = ConvertLineString(ls);
                }
                else if (geometry is MultiLineString mls)
                {
                    // Take first line for now
                    if (mls.NumGeometries > 0 && mls.GetGeometryN(0) is LineString firstLine)
                    {
                        geoGeom = ConvertLineString(firstLine);
                    }
                }

                if (geoGeom == null) continue;

                // Extract attributes from DBF
                var props = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                if (dbaseHeader != null)
                {
                    for (var i = 0; i < dbaseHeader.NumFields; i++)
                    {
                        var field = dbaseHeader.Fields[i];
                        var value = reader.GetValue(i);
                        props[field.Name] = value?.ToString() ?? "";
                    }
                }
                props["provider"] = "shapefile";
                props["source"] = Path.GetFileName(path);

                // Generate ID from record number or attributes
                var id = props.TryGetValue("id", out var idObj) 
                    ? Convert.ToString(idObj, System.Globalization.CultureInfo.InvariantCulture) ?? $"shapefile-{recordNumber}"
                    : $"shapefile-{recordNumber}";

                if (string.IsNullOrWhiteSpace(id))
                {
                    id = $"shapefile-{recordNumber}";
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

    private static GeoPolygon ConvertPolygon(Polygon poly)
    {
        var exteriorRing = poly.ExteriorRing;
        if (exteriorRing == null || exteriorRing.Coordinates.Length < 3)
            throw new InvalidOperationException("Polygon must have at least 3 points in exterior ring");

        var points = new List<GeoPoint>();
        foreach (var coord in exteriorRing.Coordinates)
        {
            points.Add(new GeoPoint
            {
                Longitude = new Longitude(coord.X),
                Latitude = new Latitude(coord.Y)
            });
        }

        // Ensure closed
        if (points.Count > 0 && 
            (Math.Abs(points[0].Latitude.Degrees - points[^1].Latitude.Degrees) > 1e-9 ||
             Math.Abs(points[0].Longitude.Degrees - points[^1].Longitude.Degrees) > 1e-9))
        {
            points.Add(points[0]);
        }

        return new GeoPolygon(points);
    }

    private static GeoPolyline ConvertLineString(LineString line)
    {
        if (line.Coordinates == null || line.Coordinates.Length < 2)
            throw new InvalidOperationException("LineString must have at least 2 points");

        var points = new List<GeoPoint>();
        foreach (var coord in line.Coordinates)
        {
            points.Add(new GeoPoint
            {
                Longitude = new Longitude(coord.X),
                Latitude = new Latitude(coord.Y)
            });
        }

        return new GeoPolyline(points);
    }
}
