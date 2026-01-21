using System.Text.Json;
using Nexo.Abstractions;

namespace Nexo.Tools.GeoTerrain;

/// <summary>
/// Tool that converts an SRTM .hgt tile to an OBJ mesh using deterministic mesh generation.
/// </summary>
public sealed class SrtmHgtToObjTool : ITool
{
    public string Id => "geoterrain.srtm.hgt.to-obj";

    public ToolSchema Schema => new(Id, "Convert an SRTM .hgt tile to an OBJ mesh file", """
    {"type":"object","required":["root","hgtPath","objPath"],"properties":{"root":{"type":"string"},"hgtPath":{"type":"string"},"objPath":{"type":"string"},"verticalScale":{"type":"number"},"treatNoDataAsZero":{"type":"boolean"}}}
    """);

    private sealed record Args(string root, string hgtPath, string objPath, float? verticalScale, bool? treatNoDataAsZero);

    public Task<ToolResult> InvokeAsync(ToolCall toolCall, WorldSnapshot s, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<Args>(toolCall.Arguments)!;
        var hgtFull = Path.Combine(args.root, args.hgtPath);
        var objFull = Path.Combine(args.root, args.objPath);

        var bytes = File.ReadAllBytes(hgtFull);
        var heights = Nexo.GeoTerrain.SrtmHgtParser.ParseHeightsMeters(bytes, out var size);

        // Derive bounds from tile name if possible; otherwise default to 0..1 degrees.
        var fileName = Path.GetFileName(args.hgtPath);
        var bounds = Nexo.GeoTerrain.SrtmTileId.TryParse(fileName, out var tile)
            ? tile.ToBounds()
            : new Nexo.GeoTerrain.GeoBounds
            {
                MinLatitude = new Nexo.GeoTerrain.Latitude(0),
                MaxLatitude = new Nexo.GeoTerrain.Latitude(1),
                MinLongitude = new Nexo.GeoTerrain.Longitude(0),
                MaxLongitude = new Nexo.GeoTerrain.Longitude(1)
            };

        // Approximate meters-per-sample using latitude at tile midpoint.
        // 1 degree latitude ≈ 111_320m; longitude scales by cos(lat).
        var midLatRad = (bounds.MinLatitude.Degrees + bounds.MaxLatitude.Degrees) * 0.5 * (Math.PI / 180.0);
        var metersPerDegLat = 111_320.0;
        var metersPerDegLon = 111_320.0 * Math.Cos(midLatRad);

        var degPerSample = 1.0 / (size - 1);
        var spacing = new Nexo.GeoTerrain.GridSpacing(
            metersX: metersPerDegLon * degPerSample,
            metersY: metersPerDegLat * degPerSample);

        var grid = new Nexo.GeoTerrain.ElevationGrid(size, size, bounds, spacing, heights);

        var mesh = Nexo.GeoTerrain.GridMeshGenerator.Generate(grid, new Nexo.GeoTerrain.MeshGenerationOptions
        {
            VerticalScale = args.verticalScale ?? 1f,
            GenerateNormals = true,
            TreatNoDataAsZero = args.treatNoDataAsZero ?? false
        });

        var obj = Nexo.GeoTerrain.ObjMeshWriter.Write(mesh);

        Directory.CreateDirectory(Path.GetDirectoryName(objFull)!);
        File.WriteAllText(objFull, obj, new System.Text.UTF8Encoding(false));

        var report = Nexo.GeoTerrain.MeshQualityAnalyzer.Analyze(grid, mesh);

        var delta = new ActionDelta(s.Tick, s.Tick + 1, new[]
        {
            $"geoterrain:hgt.to-obj in={args.hgtPath} out={args.objPath}",
            $"geoterrain:mesh.vertices={report.VertexCount}",
            $"geoterrain:mesh.triangles={report.TriangleCount}",
            $"geoterrain:grid.nodata={report.NoDataSamples}"
        });

        return Task.FromResult(new ToolResult(delta, new
        {
            ok = true,
            input = args.hgtPath,
            output = args.objPath,
            gridSize = size,
            report.VertexCount,
            report.TriangleCount,
            report.MinHeightMeters,
            report.MaxHeightMeters,
            report.NoDataSamples
        }));
    }
}

