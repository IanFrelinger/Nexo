using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.VectorTiles;
using NetTopologySuite.IO.VectorTiles.Mapbox;
using Nexo.Adapters.GeoVector.Providers;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.GeoTerrain;
using Nexo.GeoVector.Values;

namespace Nexo.Tests.Infrastructure.Tests.GeoVector;

public sealed class MapboxVectorTileProviderTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestParsesBuildingPolygonAsync(cancellationToken);

            return new TestResult
            {
                Name = nameof(MapboxVectorTileProviderTests),
                Category = "Adapters",
                Passed = true,
                Message = "Mapbox vector tile provider tests passed"
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(MapboxVectorTileProviderTests),
                Category = "Adapters",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestParsesBuildingPolygonAsync(CancellationToken ct)
    {
        var z = 15;
        var tileset = "mapbox.mapbox-streets-v8";
        var token = "pk.test-token";

        // Small bounds around (0,0) should resolve to a small number of tiles.
        var bounds = new GeoBounds
        {
            MinLatitude = new Latitude(0.0),
            MinLongitude = new Longitude(0.0),
            MaxLatitude = new Latitude(0.001),
            MaxLongitude = new Longitude(0.001)
        };

        // Compute a tile likely covering these bounds, and build a synthetic tile for that Z/X/Y.
        var centerLat = (bounds.MinLatitude.Degrees + bounds.MaxLatitude.Degrees) * 0.5;
        var centerLon = (bounds.MinLongitude.Degrees + bounds.MaxLongitude.Degrees) * 0.5;
        var (tx, ty) = LonLatToTile(centerLon, centerLat, z);
        var bytes = BuildSyntheticBuildingTileBytes(z, tx, ty, centerLon, centerLat);

        var handler = new StubHttpMessageHandler(req =>
        {
            AssertNotNull(req.RequestUri, "RequestUri should be set");
            var uri = req.RequestUri!.ToString();
            AssertTrue(uri.Contains($"/v4/{tileset}/", StringComparison.Ordinal), "Expected Mapbox v4 tile URL");
            AssertTrue(uri.Contains("access_token=", StringComparison.Ordinal), "Expected access token query");
            return bytes;
        });

        var http = new HttpClient(handler);
        var provider = new MapboxVectorTileProvider(
            http,
            accessToken: token,
            tilesetId: tileset,
            zoom: z,
            formatExtension: "mvt",
            logger: NullLogger<MapboxVectorTileProvider>.Instance);

        var got = await provider.GetFeaturesAsync(bounds, FeatureKind.Building, ct);

        // We can't guarantee the exact tile x/y used from these bounds without duplicating tile math here,
        // but we should at least get >= 0 features and never throw. For this synthetic payload we expect 1+.
        AssertTrue(got.Features.Count >= 1, "Expected at least one building feature from synthetic tile");

        var f = got.Features[0];
        AssertNotNull(f.Geometry, "Feature geometry should be present");
        var poly = f.Geometry as Nexo.GeoVector.Geometry.GeoPolygon;
        AssertTrue(poly != null, "Expected a polygon geometry for building features");
        AssertTrue(poly!.OuterRing.Count >= 4, "Polygon ring should have >= 4 points (closed)");
    }

    private static byte[] BuildSyntheticBuildingTileBytes(int z, int x, int y, double lon0, double lat0)
    {
        // Build a vector tile that encodes a building polygon. Writer will project lon/lat into tile-local coords.
        var tileDef = new NetTopologySuite.IO.VectorTiles.Tiles.Tile(x, y, z);

        var vt = new VectorTile { TileId = tileDef.Id };
        var layer = new Layer { Name = "building" };

        // A tiny square near the requested bounds center; writer will map into the requested tile.
        var gf = new GeometryFactory();
        var d = 0.00005;
        var poly = gf.CreatePolygon(new[]
        {
            new Coordinate(lon0 - d, lat0 - d),
            new Coordinate(lon0 + d, lat0 - d),
            new Coordinate(lon0 + d, lat0 + d),
            new Coordinate(lon0 - d, lat0 + d),
            new Coordinate(lon0 - d, lat0 - d)
        });

        var attrs = new AttributesTable(new Dictionary<string, object>
        {
            ["id"] = 1L,
            ["height"] = 12
        });
        layer.Features.Add(new Feature(poly, attrs));
        vt.Layers.Add(layer);

        using var ms = new MemoryStream();
        MapboxTileWriter.Write(
            vt,
            ms,
            MapboxTileWriter.DefaultMinLinealExtent,
            MapboxTileWriter.DefaultMinPolygonalExtent,
            4096,
            "id");

        return ms.ToArray();
    }

    private static (int x, int y) LonLatToTile(double lonDeg, double latDeg, int z)
    {
        latDeg = Math.Max(-85.05112878, Math.Min(85.05112878, latDeg));
        var n = Math.Pow(2.0, z);
        var x = (int)Math.Floor((lonDeg + 180.0) / 360.0 * n);

        var latRad = latDeg * Math.PI / 180.0;
        var y = (int)Math.Floor((1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * n);
        return (x, y);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, byte[]> _responder;
        public StubHttpMessageHandler(Func<HttpRequestMessage, byte[]> responder) => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var bytes = _responder(request);
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(bytes)
            };
            return Task.FromResult(resp);
        }
    }
}

