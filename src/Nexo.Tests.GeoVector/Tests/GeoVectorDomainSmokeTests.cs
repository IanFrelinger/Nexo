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
            // - Roof is duplicated (4 vertices)
            // - Walls duplicate per edge (4 edges * 4 vertices = 16)
            // Total vertices = 20
            // - Top triangulation => 2 triangles
            // - Walls => 4 edges * 2 triangles = 8 triangles
            // Total triangles = 10 => indices = 30
            AssertEqual(20, mesh.Vertices.Count, "Expected 20 vertices for a single quad building");
            AssertEqual(30, mesh.Indices.Count, "Expected 30 indices (10 triangles) for a single quad building");
            AssertNotNull(mesh.Normals, "Normals should be generated");
            AssertEqual(mesh.Vertices.Count, mesh.Normals!.Count, "Normals should match vertex count");
            AssertTrue(mesh.TexCoords is null, "TexCoords should be null when GenerateTexCoords=false");

            // Road ribbon (2-point polyline).
            var roadLine = new[]
            {
                new GeoPoint { Latitude = new Latitude(0.0000), Longitude = new Longitude(0.0000) },
                new GeoPoint { Latitude = new Latitude(0.0000), Longitude = new Longitude(0.0001) }
            };
            var road = new GeoFeature(
                id: "r1",
                kind: FeatureKind.Road,
                geometry: new GeoPolyline(roadLine),
                properties: new Dictionary<string, object> { ["class"] = "residential" });

            var roadMesh = RoadMeshGenerator.GenerateRoads(
                new GeoFeatureSet(new[] { road }),
                origin,
                terrainGrid: null,
                new RoadMeshGenerationOptions { DefaultWidthMeters = 4, GenerateTexCoords = true, UvMetersPerRepeat = 1.0f });

            AssertTrue(roadMesh.Vertices.Count >= 4, "Road mesh should have >= 4 vertices");
            AssertTrue(roadMesh.Indices.Count >= 6, "Road mesh should have indices");
            AssertNotNull(roadMesh.TexCoords, "Road mesh should generate texcoords");

            // Water surface (simple square polygon).
            var waterFeature = new GeoFeature(
                id: "w1",
                kind: FeatureKind.Water,
                geometry: new GeoPolygon(ring),
                properties: new Dictionary<string, object> { ["natural"] = "water" });

            var waterMesh = WaterMeshGenerator.GenerateWater(
                new GeoFeatureSet(new[] { waterFeature }),
                origin,
                terrainGrid: null,
                new WaterMeshGenerationOptions { GenerateTexCoords = true, UvMetersPerRepeat = 5.0f });

            AssertTrue(waterMesh.Vertices.Count >= 3, "Water mesh should have vertices");
            AssertTrue(waterMesh.Indices.Count >= 3, "Water mesh should have indices");
            AssertNotNull(waterMesh.TexCoords, "Water mesh should generate texcoords");

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

