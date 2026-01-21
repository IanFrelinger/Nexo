using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.GeoTerrain;

namespace Nexo.Tests.GeoTerrain.Tests;

public sealed class GeoTerrainDomainSmokeTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            TestValueObjects();
            TestGridMeshGeneration();

            return Task.FromResult(new TestResult
            {
                TestName = nameof(GeoTerrainDomainSmokeTests),
                Category = "GeoTerrain",
                Passed = true,
                Message = "GeoTerrain domain smoke tests passed"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                TestName = nameof(GeoTerrainDomainSmokeTests),
                Category = "GeoTerrain",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }

    private void TestValueObjects()
    {
        var lat = new Latitude(45);
        var lon = new Longitude(-122.5);
        AssertEqual(45d, lat.Degrees);
        AssertEqual(-122.5d, lon.Degrees);

        var bounds = new GeoBounds
        {
            MinLatitude = new Latitude(44),
            MaxLatitude = new Latitude(46),
            MinLongitude = new Longitude(-123),
            MaxLongitude = new Longitude(-122)
        };
        bounds.Validate();
        AssertTrue(bounds.Contains(new GeoPoint { Latitude = lat, Longitude = lon }));

        AssertEqual(ElevationUnit.Meters, ElevationUnit.FromName("meters"));
        AssertEqual(ElevationUnit.Feet, ElevationUnit.FromSymbol("FT"));
    }

    private void TestGridMeshGeneration()
    {
        var bounds = new GeoBounds
        {
            MinLatitude = new Latitude(0),
            MaxLatitude = new Latitude(1),
            MinLongitude = new Longitude(0),
            MaxLongitude = new Longitude(1)
        };

        // 2x2 grid -> 4 vertices, 2 triangles => 6 indices
        var heights = new float[]
        {
            0, 0,
            0, 0
        };
        var grid = new ElevationGrid(2, 2, bounds, new GridSpacing(1, 1), heights);
        var mesh = GridMeshGenerator.Generate(grid, new MeshGenerationOptions { GenerateNormals = true });

        AssertEqual(4, mesh.Vertices.Count);
        AssertEqual(6, mesh.Indices.Count);
        AssertNotNull(mesh.Normals);
        AssertEqual(4, mesh.Normals!.Count);

        var report = MeshQualityAnalyzer.Analyze(grid, mesh);
        AssertEqual(4, report.VertexCount);
        AssertEqual(2, report.TriangleCount);
        AssertEqual(0f, report.MinHeightMeters);
        AssertEqual(0f, report.MaxHeightMeters);
    }
}

