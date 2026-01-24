using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Adapters.GeoTerrain.Providers;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.Infrastructure.Tests.GeoTerrain;

public sealed class GeoTerrainMapboxRasterTileDownloaderTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestBuildsExpectedUrlAndReturnsBytesAsync(cancellationToken);
            return new TestResult
            {
                TestName = nameof(GeoTerrainMapboxRasterTileDownloaderTests),
                Category = "Adapters",
                Passed = true,
                Message = "Mapbox raster tile downloader tests passed"
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = nameof(GeoTerrainMapboxRasterTileDownloaderTests),
                Category = "Adapters",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestBuildsExpectedUrlAndReturnsBytesAsync(CancellationToken ct)
    {
        string? gotUrl = null;
        var expected = new byte[] { 1, 2, 3, 4 };
        var handler = new StubHttpMessageHandler(req =>
        {
            gotUrl = req.RequestUri?.ToString();
            return expected;
        });
        var http = new HttpClient(handler);
        var dl = new MapboxRasterTileDownloader(http, accessToken: "tok", logger: NullLogger<MapboxRasterTileDownloader>.Instance);

        var bytes = await dl.DownloadAsync("mapbox.satellite", z: 1, x: 2, y: 3, format: "png", ct);
        AssertTrue(bytes.Length == expected.Length, "Expected byte length to match");
        AssertTrue(gotUrl != null && gotUrl.Contains("/mapbox.satellite/1/2/3.png"), "Expected URL to contain tile path");
        AssertTrue(gotUrl != null && gotUrl.Contains("access_token="), "Expected URL to include access_token");
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

