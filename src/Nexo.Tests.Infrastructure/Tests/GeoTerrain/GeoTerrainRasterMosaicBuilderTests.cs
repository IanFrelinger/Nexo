using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Adapters.GeoTerrain.Providers;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.GeoTerrain;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Nexo.Tests.Infrastructure.Tests.GeoTerrain;

public sealed class GeoTerrainRasterMosaicBuilderTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestBuildsMosaicDimensionsAsync(cancellationToken);
            return new TestResult
            {
                TestName = nameof(GeoTerrainRasterMosaicBuilderTests),
                Category = "Adapters",
                Passed = true,
                Message = "Raster mosaic builder tests passed"
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = nameof(GeoTerrainRasterMosaicBuilderTests),
                Category = "Adapters",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestBuildsMosaicDimensionsAsync(CancellationToken ct)
    {
        // Use bounds that map to multiple tiles at a low zoom.
        var bounds = new GeoBounds
        {
            MinLatitude = new Latitude(0),
            MinLongitude = new Longitude(0),
            MaxLatitude = new Latitude(1),
            MaxLongitude = new Longitude(1)
        };
        bounds.Validate();

        // Stub downloader: return a 4x4 PNG for every tile.
        var png = MakePng(4, 4);
        var handler = new StubHttpMessageHandler(_ => png);
        var http = new HttpClient(handler);
        var dl = new MapboxRasterTileDownloader(http, accessToken: "tok", logger: NullLogger<MapboxRasterTileDownloader>.Instance);
        var builder = new MapboxRasterMosaicBuilder(dl, logger: NullLogger<MapboxRasterMosaicBuilder>.Instance);

        var mosaic = await builder.BuildAsync(bounds, zoom: 1, tilesetId: "mapbox.satellite", format: "png", ct);
        AssertTrue(mosaic.TileSizePixels == 4, "Expected tile size 4");
        AssertTrue(mosaic.TilesWide >= 1, "Expected at least 1 tile wide");
        AssertTrue(mosaic.TilesHigh >= 1, "Expected at least 1 tile high");

        using var img = Image.Load<Rgba32>(mosaic.PngBytes);
        AssertEqual(mosaic.TilesWide * mosaic.TileSizePixels, img.Width);
        AssertEqual(mosaic.TilesHigh * mosaic.TileSizePixels, img.Height);
    }

    private static byte[] MakePng(int w, int h)
    {
        using var img = new Image<Rgba32>(w, h);
        img.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < accessor.Width; x++)
                {
                    row[x] = new Rgba32(10, 20, 30, 255);
                }
            }
        });
        using var ms = new MemoryStream();
        img.SaveAsPng(ms);
        return ms.ToArray();
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, byte[]> _responseFactory;
        public StubHttpMessageHandler(Func<HttpRequestMessage, byte[]> responseFactory) => _responseFactory = responseFactory;

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

