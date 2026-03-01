using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Adapters.GeoTerrain.Providers;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Nexo.Tests.Infrastructure.Tests.GeoTerrain;

public sealed class GeoTerrainTerrainRgbProviderTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestDecodes2x2TileAsync(cancellationToken);
            return new TestResult
            {
                Name = nameof(GeoTerrainTerrainRgbProviderTests),
                Category = "Adapters",
                Passed = true,
                Message = "Terrain-RGB provider tests passed"
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(GeoTerrainTerrainRgbProviderTests),
                Category = "Adapters",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestDecodes2x2TileAsync(CancellationToken ct)
    {
        // Build a 2x2 PNG in-memory with deterministic pixel values.
        byte[] png;
        using (var img = new Image<Rgba32>(2, 2))
        {
            img[0, 0] = new Rgba32(0, 0, 0);         // -10000
            img[1, 0] = new Rgba32(0, 0, 10);        // -9999
            img[0, 1] = new Rgba32(0, 1, 0);         // -9974.4
            img[1, 1] = new Rgba32(0, 1, 255);       // -9948.9
            using var ms = new MemoryStream();
            await img.SaveAsPngAsync(ms, ct);
            png = ms.ToArray();
        }

        var handler = new StubHttpMessageHandler(_ => png);
        var http = new HttpClient(handler);
        var provider = new MapboxTerrainRgbGridProvider(http, accessToken: "token", tilesetId: "mapbox.terrain-rgb", logger: NullLogger<MapboxTerrainRgbGridProvider>.Instance);

        var (grid, bytes) = await provider.GetTileGridAsync(z: 0, x: 0, y: 0, ct);
        AssertEqual(2, grid.Width, "Expected width=2");
        AssertEqual(2, grid.Height, "Expected height=2");
        AssertTrue(bytes.Length == png.Length, "Expected returned png bytes to match");

        // Validate some decoded heights (approx).
        AssertEqual(-10000f, grid.GetHeightMeters(0, 0), "Expected -10000m for 0,0,0");
        AssertEqual(-9999f, grid.GetHeightMeters(1, 0), "Expected -9999m for 0,0,10");
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, byte[]> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, byte[]> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var bytes = _responseFactory(request);
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(bytes)
            });
        }
    }
}

