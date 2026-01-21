using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.GeoTerrain;
using Nexo.GeoVector.Generation;
using Nexo.GeoVector.Geometry;
using Nexo.GeoVector.Models;
using Nexo.GeoVector.Values;

namespace Nexo.Tests.GeoVector.Tests;

public sealed class GeoVectorDomainSmokeTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var origin = new GeoPoint { Latitude = new Latitude(0), Longitude = new Longitude(0) };

            // Square building footprint around origin (degrees, tiny area).
            var ring = new[]
            {
                new GeoPoint { Latitude = new Latitude(0.0000), Longitude = new Longitude(0.0000) },
                new GeoPoint { Latitude = new Latitude(0.0000), Longitude = new Longitude(0.0001) },
                new GeoPoint { Latitude = new Latitude(0.0001), Longitude = new Longitude(0.0001) },
                new GeoPoint { Latitude = new Latitude(0.0001), Longitude = new Longitude(0.0000) },
                new GeoPoint { Latitude = new Latitude(0.0000), Longitude = new Longitude(0.0000) } // closed
            };

            var feature = new GeoFeature(
                id: "b1",
                kind: FeatureKind.Building,
                geometry: new GeoPolygon(ring),
                properties: new Dictionary<string, object> { ["height_m"] = 12f });

            var mesh = BuildingMeshGenerator.GenerateBuildings(
                new GeoFeatureSet(new[] { feature }),
                origin,
                new BuildingExtrusionOptions { IncludeBottom = false });

            // For a quad footprint:
            // - 4 base + 4 top vertices = 8 vertices
            // - Top triangulation => 2 triangles
            // - Walls => 4 edges * 2 triangles = 8 triangles
            // Total triangles = 10 => indices = 30
            AssertEqual(8, mesh.Vertices.Count, "Expected 8 vertices for a single quad building");
            AssertEqual(30, mesh.Indices.Count, "Expected 30 indices (10 triangles) for a single quad building");
            AssertNotNull(mesh.Normals, "Normals should be generated");
            AssertEqual(mesh.Vertices.Count, mesh.Normals!.Count, "Normals should match vertex count");

            return Task.FromResult(new TestResult
            {
                TestName = nameof(GeoVectorDomainSmokeTests),
                Category = "Tests",
                Passed = true,
                Message = "GeoVector domain smoke tests passed"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                TestName = nameof(GeoVectorDomainSmokeTests),
                Category = "Tests",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }
}

