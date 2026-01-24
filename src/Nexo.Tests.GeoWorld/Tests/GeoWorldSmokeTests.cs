using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.GeoTerrain;
using Nexo.GeoVector.Models;
using Nexo.GeoVector.Values;
using Nexo.GeoWorld.Placement;
using Nexo.GeoWorld.Materials;
using Nexo.GeoWorld.Vegetation;
using Local2 = Nexo.GeoVector.Geometry.Vector2;
using Nexo.GeoVector.Geometry;

namespace Nexo.Tests.GeoWorld.Tests;

public sealed class GeoWorldSmokeTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            TestMaskEvaluator();
            await TestVegetationPlacementAsync(cancellationToken);
            TestMaterialInference();

            return new TestResult
            {
                TestName = nameof(GeoWorldSmokeTests),
                Category = "GeoWorld",
                Passed = true,
                Message = "GeoWorld smoke tests passed"
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = nameof(GeoWorldSmokeTests),
                Category = "GeoWorld",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private void TestMaskEvaluator()
    {
        var square = new[]
        {
            new Local2(0, 0),
            new Local2(10, 0),
            new Local2(10, 10),
            new Local2(0, 10),
            new Local2(0, 0)
        };

        var includes = new IPlacementMask[] { new PolygonMask(square) };
        var excludes = new IPlacementMask[] { new PolygonMask(new[] { new Local2(4, 4), new Local2(6, 4), new Local2(6, 6), new Local2(4, 6), new Local2(4, 4) }) };
        var eval = new MaskEvaluator(includes, excludes);

        AssertTrue(eval.IsAllowed(new Local2(1, 1)), "Point inside include should be allowed");
        AssertTrue(!eval.IsAllowed(new Local2(5, 5)), "Point inside exclude should be rejected");
        AssertTrue(!eval.IsAllowed(new Local2(20, 20)), "Point outside include should be rejected");
    }

    private Task TestVegetationPlacementAsync(CancellationToken ct)
    {
        // 10x10 grid, 1m spacing, flat at 0m.
        var bounds = new GeoBounds
        {
            MinLatitude = new Latitude(0),
            MinLongitude = new Longitude(0),
            MaxLatitude = new Latitude(0.001),
            MaxLongitude = new Longitude(0.001)
        };

        var heights = new float[11 * 11];
        for (var i = 0; i < heights.Length; i++) heights[i] = 0f;
        var grid = new ElevationGrid(width: 11, height: 11, bounds: bounds, spacing: new GridSpacing(1, 1), heightsMeters: heights);

        // Include everything (single vegetation polygon).
        var ring = new[]
        {
            new GeoPoint { Latitude = new Latitude(0.0001), Longitude = new Longitude(0.0001) },
            new GeoPoint { Latitude = new Latitude(0.0001), Longitude = new Longitude(0.0009) },
            new GeoPoint { Latitude = new Latitude(0.0009), Longitude = new Longitude(0.0009) },
            new GeoPoint { Latitude = new Latitude(0.0009), Longitude = new Longitude(0.0001) },
            new GeoPoint { Latitude = new Latitude(0.0001), Longitude = new Longitude(0.0001) }
        };
        var veg = new GeoFeatureSet(new[]
        {
            new GeoFeature("veg-1", FeatureKind.Vegetation, new GeoPolygon(ring))
        });

        var options = new VegetationPlacementOptions
        {
            Seed = 1,
            InstancesPerSquareKm = 50_000f, // tiny grid -> a handful
            MinInstanceSpacingMeters = 1.0f,
            MaxSlopeDegrees = 45f,
            Species = new[]
            {
                new VegetationSpecies { Kind = "tree", Weight = 1.0f, MinUniformScale = 0.9f, MaxUniformScale = 1.1f },
                new VegetationSpecies { Kind = "bush", Weight = 1.0f, MinUniformScale = 0.4f, MaxUniformScale = 0.6f }
            }
        };

        var instances = VegetationPlacer.Place(grid, veg, buildingPolygons: null, waterPolygons: null, roadLines: null, options);
        AssertTrue(instances.Count > 0, "Expected some instances");
        AssertTrue(instances.All(i => i.Position.X >= 0 && i.Position.Z >= 0), "Instances should be within local grid");
        AssertTrue(instances.All(i => i.Position.Y == 0f), "Flat grid -> Y=0");
        AssertTrue(instances.Any(i => i.Kind == "tree") && instances.Any(i => i.Kind == "bush"), "Expected multiple kinds");

        return Task.CompletedTask;
    }

    private void TestMaterialInference()
    {
        var buildings = new GeoFeatureSet(new[]
        {
            new GeoFeature("b1", FeatureKind.Building, new GeoPolygon(new[]
            {
                new GeoPoint { Latitude = new Latitude(0), Longitude = new Longitude(0) },
                new GeoPoint { Latitude = new Latitude(0), Longitude = new Longitude(0.0001) },
                new GeoPoint { Latitude = new Latitude(0.0001), Longitude = new Longitude(0.0001) },
                new GeoPoint { Latitude = new Latitude(0.0001), Longitude = new Longitude(0) },
                new GeoPoint { Latitude = new Latitude(0), Longitude = new Longitude(0) }
            }),
            new Dictionary<string, object> { ["building"] = "industrial" })
        });

        var mats = MaterialInference.InferForBuildings(buildings, uvMetersPerRepeat: 2.0f);
        AssertTrue(mats.Count == 1, "Expected one material assignment for buildings");
        AssertEqual("buildings", mats[0].MeshKind, "MeshKind should match");
        AssertEqual("building_industrial", mats[0].MaterialName, "Expected industrial inference");
        AssertTrue(mats[0].UvMetersPerRepeat.HasValue, "Expected UV meters per repeat to be set");
    }
}

