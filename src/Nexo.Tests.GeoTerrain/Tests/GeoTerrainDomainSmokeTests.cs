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
            TestContours();

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

    private void TestContours()
    {
        var bounds = new GeoBounds
        {
            MinLatitude = new Latitude(0),
            MaxLatitude = new Latitude(1),
            MinLongitude = new Longitude(0),
            MaxLongitude = new Longitude(1)
        };

        // 2x2 grid (single cell). Bottom row 0m, top row 20m => a single contour at 10m across mid-row.
        var heights = new float[]
        {
            0, 0,
            20, 20
        };

        var grid = new ElevationGrid(2, 2, bounds, new GridSpacing(1, 1), heights);
        var contours = ContourGenerator.Generate(grid, new ContourGenerationOptions
        {
            IntervalMeters = 10,
            MinElevationMeters = 10,
            MaxElevationMeters = 10,
            VerticalScale = 1.0f
        });

        AssertEqual(1, contours.Count);
        AssertEqual(10f, contours[0].ElevationMeters);
        AssertEqual(2, contours[0].Points.Count);

        var a = contours[0].Points[0];
        var b = contours[0].Points[1];

        // Expect a horizontal line at z=0.5 from x=0..1, with y=10.
        AssertEqual(10f, a.Y);
        AssertEqual(10f, b.Y);
        AssertEqual(0.5f, a.Z);
        AssertEqual(0.5f, b.Z);
        AssertTrue((a.X == 0f && b.X == 1f) || (a.X == 1f && b.X == 0f), "Expected contour endpoints at x=0 and x=1");
    }
}

