using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.GeoTerrain;
using Nexo.SDK.Estimation;

namespace Nexo.Tests.Infrastructure.Tests.SDK;

/// <summary>
/// Tests for SDK caching functionality and resource estimation with caching.
/// </summary>
public sealed class CachingSdkTests : UnitTestBase
{
    private string? _testCacheDir;

    public override Task SetupAsync(CancellationToken cancellationToken = default)
    {
        _testCacheDir = Path.Combine(Path.GetTempPath(), $"nexo-sdk-cache-{Guid.NewGuid()}");
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
            
            await TestResourceEstimationWithPartialCacheAsync(cancellationToken);
            await TestResourceEstimationWithAllCachedAsync(cancellationToken);
            await TestResourceEstimationWithNoCacheAsync(cancellationToken);
            await TestResourceEstimationWithFromCacheTrueAsync(cancellationToken);

            return new TestResult
            {
                TestName = nameof(CachingSdkTests),
                Category = "SDK",
                Passed = true,
                Message = "SDK caching tests passed"
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = nameof(CachingSdkTests),
                Category = "SDK",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestResourceEstimationWithPartialCacheAsync(CancellationToken ct)
    {
        // Arrange
        var estimator = new ResourceEstimationService();
        var cacheRoot = Path.Combine(_testCacheDir!, "estimation");
        var srtmCacheDir = Path.Combine(cacheRoot, "srtm");
        Directory.CreateDirectory(srtmCacheDir);

        var cachedTileId = new SrtmTileId(37, -122);
        var uncachedTileId = new SrtmTileId(37, -121);

        // Create one cached tile
        var cachedPath = Path.Combine(srtmCacheDir, $"{cachedTileId}.hgt");
        await File.WriteAllBytesAsync(cachedPath, new byte[2_884_802], ct);

        var tileIds = new[] { cachedTileId, uncachedTileId };

        // Act
        var result = estimator.EstimateSrtmTileDownload(tileIds, fromCache: false, cacheRoot: cacheRoot);

        // Assert
        AssertNotNull(result, "Result should not be null");
        AssertNotNull(result.Metadata, "Metadata should not be null");
        AssertEqual(1, result.Metadata!["cached-count"], "Should detect one cached tile");
        AssertEqual(1, result.Metadata["download-count"], "Should detect one tile to download");
        AssertTrue(result.CostUsd > 0, "Should have cost for uncached tile");
    }

    private async Task TestResourceEstimationWithAllCachedAsync(CancellationToken ct)
    {
        // Arrange
        var estimator = new ResourceEstimationService();
        var cacheRoot = Path.Combine(_testCacheDir!, "all-cached");
        var srtmCacheDir = Path.Combine(cacheRoot, "srtm");
        Directory.CreateDirectory(srtmCacheDir);

        var tileId1 = new SrtmTileId(37, -122);
        var tileId2 = new SrtmTileId(37, -121);

        // Create both tiles in cache
        await File.WriteAllBytesAsync(Path.Combine(srtmCacheDir, $"{tileId1}.hgt"), new byte[2_884_802], ct);
        await File.WriteAllBytesAsync(Path.Combine(srtmCacheDir, $"{tileId2}.hgt"), new byte[2_884_802], ct);

        var tileIds = new[] { tileId1, tileId2 };

        // Act
        var result = estimator.EstimateSrtmTileDownload(tileIds, fromCache: false, cacheRoot: cacheRoot);

        // Assert
        AssertNotNull(result, "Result should not be null");
        AssertNotNull(result.Metadata, "Metadata should not be null");
        AssertEqual(2, result.Metadata!["cached-count"], "Should detect both tiles as cached");
        AssertEqual(0, result.Metadata["download-count"], "Should have no tiles to download");
        AssertEqual(0, result.CostUsd, "All cached tiles should have zero cost");
    }

    private Task TestResourceEstimationWithNoCacheAsync(CancellationToken ct)
    {
        // Arrange
        var estimator = new ResourceEstimationService();
        var tileIds = new[] { new SrtmTileId(37, -122), new SrtmTileId(37, -121) };

        // Act
        var result = estimator.EstimateSrtmTileDownload(tileIds, fromCache: false, cacheRoot: null);

        // Assert
        AssertNotNull(result, "Result should not be null");
        AssertNotNull(result.Metadata, "Metadata should not be null");
        AssertEqual(0, result.Metadata!["cached-count"], "Should have no cached tiles");
        AssertEqual(2, result.Metadata["download-count"], "Should estimate all as downloads");
        AssertTrue(result.CostUsd > 0, "Should have cost for downloads");
        
        return Task.CompletedTask;
    }

    private Task TestResourceEstimationWithFromCacheTrueAsync(CancellationToken ct)
    {
        // Arrange
        var estimator = new ResourceEstimationService();
        var cacheRoot = Path.Combine(_testCacheDir!, "from-cache");
        var tileIds = new[] { new SrtmTileId(37, -122) };

        // Act
        var result = estimator.EstimateSrtmTileDownload(tileIds, fromCache: true, cacheRoot: cacheRoot);

        // Assert
        AssertNotNull(result, "Result should not be null");
        AssertEqual(0, result.CostUsd, "fromCache=true should return zero cost");
        AssertEqual(1, result.Metadata!["cached-count"], "Should mark all as cached");
        
        return Task.CompletedTask;
    }
}
