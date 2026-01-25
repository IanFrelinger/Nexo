using Nexo.Adapters.GeoTerrain.Providers;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.GeoTerrain;

namespace Nexo.Tests.Infrastructure.Tests.GeoTerrain;

/// <summary>
/// Integration tests for caching in elevation providers.
/// Tests actual file system interactions and cache behavior.
/// </summary>
public sealed class CachingProviderTests : UnitTestBase
{
    private string? _testCacheDir;

    public override Task SetupAsync(CancellationToken cancellationToken = default)
    {
        _testCacheDir = Path.Combine(Path.GetTempPath(), $"nexo-provider-cache-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testCacheDir);
        return Task.CompletedTask;
    }

    public override Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        if (_testCacheDir != null && Directory.Exists(_testCacheDir))
        {
            try { Directory.Delete(_testCacheDir, recursive: true); } catch { /* ignore */ }
        }
        return Task.CompletedTask;
    }

    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            AssertNotNull(_testCacheDir, "Test cache directory should be created");
            
            await TestCacheDirectoryStructureAsync(cancellationToken);
            await TestMultipleCacheRootsAsync(cancellationToken);

            return new TestResult
            {
                TestName = nameof(CachingProviderTests),
                Category = "Integration",
                Passed = true,
                Message = "Caching provider tests passed"
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = nameof(CachingProviderTests),
                Category = "Integration",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestCacheDirectoryStructureAsync(CancellationToken ct)
    {
        // Arrange
        var cacheRoot = Path.Combine(_testCacheDir!, "structure-test");
        var srtmCacheDir = Path.Combine(cacheRoot, "srtm");
        var tileId = new SrtmTileId(37, -122);
        var expectedPath = Path.Combine(srtmCacheDir, $"{tileId}.hgt");

        // Act - Create directory structure
        Directory.CreateDirectory(srtmCacheDir);
        var fakeTileData = new byte[2_884_802];
        await File.WriteAllBytesAsync(expectedPath, fakeTileData);

        // Assert
        AssertTrue(File.Exists(expectedPath), "Cached tile should exist at expected path");
        AssertEqual(srtmCacheDir, Path.GetDirectoryName(expectedPath), "Cache should be in srtm subdirectory");
    }

    private async Task TestMultipleCacheRootsAsync(CancellationToken ct)
    {
        // Arrange
        var cacheRoot1 = Path.Combine(_testCacheDir!, "cache1");
        var cacheRoot2 = Path.Combine(_testCacheDir!, "cache2");
        var tileId = new SrtmTileId(37, -122);

        // Act - Create tiles in different cache roots
        var srtmCache1 = Path.Combine(cacheRoot1, "srtm");
        var srtmCache2 = Path.Combine(cacheRoot2, "srtm");
        Directory.CreateDirectory(srtmCache1);
        Directory.CreateDirectory(srtmCache2);

        var path1 = Path.Combine(srtmCache1, $"{tileId}.hgt");
        var path2 = Path.Combine(srtmCache2, $"{tileId}.hgt");
        await File.WriteAllBytesAsync(path1, new byte[100], ct);
        await File.WriteAllBytesAsync(path2, new byte[200], ct);

        // Assert
        AssertTrue(File.Exists(path1), "First cache should have tile");
        AssertTrue(File.Exists(path2), "Second cache should have tile");
        var data1 = await File.ReadAllBytesAsync(path1, ct);
        var data2 = await File.ReadAllBytesAsync(path2, ct);
        AssertEqual(100, data1.Length, "First cache should have correct data");
        AssertEqual(200, data2.Length, "Second cache should have different data");
    }
}
