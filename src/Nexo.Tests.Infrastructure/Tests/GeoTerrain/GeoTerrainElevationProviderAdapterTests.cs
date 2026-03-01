using Microsoft.Extensions.DependencyInjection;
using Nexo.Adapters.GeoTerrain;
using Nexo.GeoTerrain;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Orchestration.GeoTerrain.Ports;

namespace Nexo.Tests.Infrastructure.Tests.GeoTerrain;

public sealed class GeoTerrainElevationProviderAdapterTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestLocalProviderLoadsTileAsync(cancellationToken);
            await TestCacheProviderReturnsSameBytesAsync(cancellationToken);
            await TestHybridDownloadsAndPersistsAsync(cancellationToken);

            return new TestResult
            {
                Name = nameof(GeoTerrainElevationProviderAdapterTests),
                Category = "Adapters",
                Passed = true,
                Message = "GeoTerrain elevation provider adapter tests passed"
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(GeoTerrainElevationProviderAdapterTests),
                Category = "Adapters",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestLocalProviderLoadsTileAsync(CancellationToken ct)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "nexo-geoterrain-adapters", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);

        var tile = new SrtmTileId(0, 0);
        var file = Path.Combine(tmp, tile + ".hgt");

        // 2x2 samples: 10,20,30,40.
        await File.WriteAllBytesAsync(file, new byte[]
        {
            0x00, 0x0A,
            0x00, 0x14,
            0x00, 0x1E,
            0x00, 0x28
        }, ct);

        var services = new ServiceCollection();
        services.AddElevationProviders(o =>
        {
            o.Provider = ElevationProviderType.Local;
            o.LocalRootPath = tmp;
            o.EnableCache = false;
        });
        var sp = services.BuildServiceProvider();

        var provider = sp.GetRequiredService<IElevationProvider>();
        var got = await provider.GetSrtmTileAsync(tile, ct);

        AssertEqual(2, got.Size);
        AssertEqual((short)10, got.MinMeters);
        AssertEqual((short)40, got.MaxMeters);
    }

    private async Task TestCacheProviderReturnsSameBytesAsync(CancellationToken ct)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "nexo-geoterrain-adapters", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);

        var tile = new SrtmTileId(0, 0);
        var file = Path.Combine(tmp, tile + ".hgt");

        // 2x2 samples: 0.
        await File.WriteAllBytesAsync(file, new byte[]
        {
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00
        }, ct);

        var services = new ServiceCollection();
        services.AddElevationProviders(o =>
        {
            o.Provider = ElevationProviderType.Local;
            o.LocalRootPath = tmp;
            o.EnableCache = true;
        });
        var sp = services.BuildServiceProvider();

        var provider = sp.GetRequiredService<IElevationProvider>();
        var a = await provider.GetSrtmTileAsync(tile, ct);
        var b = await provider.GetSrtmTileAsync(tile, ct);

        AssertTrue(a.HgtBytes.Length == b.HgtBytes.Length, "Expected same byte length from cache");
        AssertTrue(a.HgtBytes[0] == b.HgtBytes[0], "Expected same byte content from cache");
    }

    private async Task TestHybridDownloadsAndPersistsAsync(CancellationToken ct)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "nexo-geoterrain-adapters", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);

        var tile = new SrtmTileId(0, 0);
        var expectedFile = Path.Combine(tmp, tile + ".hgt");
        AssertTrue(!File.Exists(expectedFile), "Precondition: tile should not exist locally");

        // Stub HTTP: return a valid 2x2 HGT payload (10,20,30,40).
        var handler = new StubHttpMessageHandler(_ =>
        {
            return new byte[]
            {
                0x00, 0x0A,
                0x00, 0x14,
                0x00, 0x1E,
                0x00, 0x28
            };
        });
        var http = new HttpClient(handler);
        var httpProvider = new Nexo.Adapters.GeoTerrain.Providers.SrtmHttpElevationProvider(
            http,
            "https://example.test/srtm/",
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Nexo.Adapters.GeoTerrain.Providers.SrtmHttpElevationProvider>.Instance);

        var localProvider = new Nexo.Adapters.GeoTerrain.Providers.LocalFileElevationProvider(tmp);
        var hybrid = new Nexo.Adapters.GeoTerrain.Providers.HybridLocalThenHttpElevationProvider(
            localProvider,
            httpProvider,
            persistToLocal: true,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Nexo.Adapters.GeoTerrain.Providers.HybridLocalThenHttpElevationProvider>.Instance);

        var got = await hybrid.GetSrtmTileAsync(tile, ct);
        AssertEqual((short)10, got.MinMeters);
        AssertEqual((short)40, got.MaxMeters);

        AssertTrue(File.Exists(expectedFile), "Hybrid provider should persist downloaded tile locally");
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, byte[]> _responder;
        public StubHttpMessageHandler(Func<HttpRequestMessage, byte[]> responder) => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var bytes = _responder(request);
            var resp = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(bytes)
            };
            return Task.FromResult(resp);
        }
    }
}

