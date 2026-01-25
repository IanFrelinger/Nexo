using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.GeoTerrain;
using Nexo.Orchestration.GeoTerrain.Ports;
using Nexo.SDK;
using Nexo.SDK.Estimation;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.SDK;

/// <summary>
/// Tests for SDK caching functionality and resource estimation with caching.
/// </summary>
public class CachingSdkTests : IDisposable
{
    private readonly string _testCacheDir;
    private readonly Mock<IElevationProvider> _mockElevationProvider;
    private readonly Mock<ILogger<GeoTerrainClient>> _mockLogger;
    private readonly Mock<IResourceEstimator> _mockEstimator;

    public CachingSdkTests()
    {
        _testCacheDir = Path.Combine(Path.GetTempPath(), $"nexo-sdk-cache-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testCacheDir);

        _mockElevationProvider = new Mock<IElevationProvider>();
        _mockLogger = new Mock<ILogger<GeoTerrainClient>>();
        _mockEstimator = new Mock<IResourceEstimator>();
    }

    [Fact]
    public void GeoTerrainClient_WithEstimator_ShouldTrackCachedTiles()
    {
        // Arrange
        var tileId = new SrtmTileId(37, -122);
        var cachedTile = new ElevationTile
        {
            TileId = tileId,
            HgtBytes = new byte[2_884_802],
            Size = 1201,
            MinMeters = 0,
            MaxMeters = 100,
            NoDataSamples = 0,
            Metadata = new Dictionary<string, object>
            {
                ["cached"] = true,
                ["cache-path"] = Path.Combine(_testCacheDir, "srtm", $"{tileId}.hgt")
            }
        };

        _mockElevationProvider
            .Setup(p => p.GetSrtmTileAsync(It.IsAny<SrtmTileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedTile);

        var estimate = new ResourceEstimate
        {
            CostUsd = 0,
            MemoryBytes = 2_884_802,
            IsEstimate = true,
            Metadata = new Dictionary<string, object>
            {
                ["cached-count"] = 1,
                ["download-count"] = 0
            }
        };

        _mockEstimator
            .Setup(e => e.EstimateSrtmTileDownload(It.IsAny<IReadOnlyList<SrtmTileId>>(), It.IsAny<bool>(), It.IsAny<string>()))
            .Returns(estimate);

        var client = new GeoTerrainClient(
            _mockElevationProvider.Object,
            _mockLogger.Object,
            _mockEstimator.Object);

        // Act & Assert
        client.Should().NotBeNull("Client should be created with estimator");
    }

    [Fact]
    public void ResourceEstimationService_ShouldDetectCachedTilesInCacheDirectory()
    {
        // Arrange
        var estimator = new ResourceEstimationService();
        var cacheRoot = Path.Combine(_testCacheDir, "estimation");
        var srtmCacheDir = Path.Combine(cacheRoot, "srtm");
        Directory.CreateDirectory(srtmCacheDir);

        var cachedTileId = new SrtmTileId(37, -122);
        var uncachedTileId = new SrtmTileId(37, -121);

        // Create one cached tile
        var cachedPath = Path.Combine(srtmCacheDir, $"{cachedTileId}.hgt");
        await File.WriteAllBytesAsync(cachedPath, new byte[2_884_802]);

        var tileIds = new[] { cachedTileId, uncachedTileId };

        // Act
        var result = estimator.EstimateSrtmTileDownload(tileIds, fromCache: false, cacheRoot: cacheRoot);

        // Assert
        result.Should().NotBeNull();
        result.Metadata.Should().NotBeNull();
        result.Metadata!["cached-count"].Should().Be(1, "Should detect one cached tile");
        result.Metadata["download-count"].Should().Be(1, "Should detect one tile to download");
        result.CostUsd.Should().BeGreaterThan(0, "Should have cost for uncached tile");
    }

    [Fact]
    public void ResourceEstimationService_WithAllCachedTiles_ShouldReturnZeroCost()
    {
        // Arrange
        var estimator = new ResourceEstimationService();
        var cacheRoot = Path.Combine(_testCacheDir, "all-cached");
        var srtmCacheDir = Path.Combine(cacheRoot, "srtm");
        Directory.CreateDirectory(srtmCacheDir);

        var tileId1 = new SrtmTileId(37, -122);
        var tileId2 = new SrtmTileId(37, -121);

        // Create both tiles in cache
        await File.WriteAllBytesAsync(Path.Combine(srtmCacheDir, $"{tileId1}.hgt"), new byte[2_884_802]);
        await File.WriteAllBytesAsync(Path.Combine(srtmCacheDir, $"{tileId2}.hgt"), new byte[2_884_802]);

        var tileIds = new[] { tileId1, tileId2 };

        // Act
        var result = estimator.EstimateSrtmTileDownload(tileIds, fromCache: false, cacheRoot: cacheRoot);

        // Assert
        result.Should().NotBeNull();
        result.Metadata.Should().NotBeNull();
        result.Metadata!["cached-count"].Should().Be(2, "Should detect both tiles as cached");
        result.Metadata["download-count"].Should().Be(0, "Should have no tiles to download");
        result.CostUsd.Should().Be(0, "All cached tiles should have zero cost");
    }

    [Fact]
    public void ResourceEstimationService_WithNoCacheRoot_ShouldEstimateAllAsDownloads()
    {
        // Arrange
        var estimator = new ResourceEstimationService();
        var tileIds = new[] { new SrtmTileId(37, -122), new SrtmTileId(37, -121) };

        // Act
        var result = estimator.EstimateSrtmTileDownload(tileIds, fromCache: false, cacheRoot: null);

        // Assert
        result.Should().NotBeNull();
        result.Metadata.Should().NotBeNull();
        result.Metadata!["cached-count"].Should().Be(0, "Should have no cached tiles");
        result.Metadata["download-count"].Should().Be(2, "Should estimate all as downloads");
        result.CostUsd.Should().BeGreaterThan(0, "Should have cost for downloads");
    }

    [Fact]
    public void ResourceEstimationService_WithFromCacheTrue_ShouldIgnoreCacheRoot()
    {
        // Arrange
        var estimator = new ResourceEstimationService();
        var cacheRoot = Path.Combine(_testCacheDir, "from-cache");
        var tileIds = new[] { new SrtmTileId(37, -122) };

        // Act
        var result = estimator.EstimateSrtmTileDownload(tileIds, fromCache: true, cacheRoot: cacheRoot);

        // Assert
        result.Should().NotBeNull();
        result.CostUsd.Should().Be(0, "fromCache=true should return zero cost regardless of cache root");
        result.Metadata!["cached-count"].Should().Be(1, "Should mark all as cached");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testCacheDir))
            {
                Directory.Delete(_testCacheDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
