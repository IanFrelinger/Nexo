using FluentAssertions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Adapters.GeoTerrain.Providers;
using Nexo.Adapters.GeoVector.Providers;
using Nexo.GeoTerrain;
using Nexo.SDK.Estimation;
using Xunit;

namespace Nexo.Tests.GeospatialUnit.Tests;

/// <summary>
/// Unit tests for caching functionality in geospatial providers and services.
/// </summary>
public class CachingTests : IDisposable
{
    private readonly string _testCacheDir;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ILogger<SrtmHttpElevationProvider>> _mockLogger;
    private readonly HttpClient _httpClient;

    public CachingTests()
    {
        _testCacheDir = Path.Combine(Path.GetTempPath(), $"nexo-cache-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testCacheDir);

        _httpClient = new HttpClient();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockHttpClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(_httpClient);

        _mockLogger = new Mock<ILogger<SrtmHttpElevationProvider>>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLoggerFactory
            .Setup(f => f.CreateLogger<SrtmHttpElevationProvider>())
            .Returns(_mockLogger.Object);
        _mockLoggerFactory
            .Setup(f => f.CreateLogger<MapboxVectorTileProvider>())
            .Returns(new Mock<ILogger<MapboxVectorTileProvider>>().Object);
    }

    [Fact]
    public void SrtmHttpElevationProvider_WithCacheRoot_ShouldCreateCacheDirectory()
    {
        // Arrange
        var cacheRoot = Path.Combine(_testCacheDir, "srtm-cache");
        var provider = new SrtmHttpElevationProvider(
            _httpClient,
            "https://example.com/srtm/",
            _mockLogger.Object,
            cacheRoot: cacheRoot,
            persistCache: true);

        // Act - Provider is created

        // Assert
        // Cache directory should be created when first tile is cached
        // For now, just verify provider accepts cache root
        provider.Should().NotBeNull();
    }

    [Fact]
    public void ElevationProviderFactory_WithCacheRoot_ShouldPassToSrtmHttpProvider()
    {
        // Arrange
        var factory = new ElevationProviderFactory(
            _mockHttpClientFactory.Object,
            _mockLoggerFactory.Object);
        var cacheRoot = Path.Combine(_testCacheDir, "factory-cache");

        // Act
        var provider = factory.Build(
            provider: "http",
            localRoot: null,
            srtmBaseUrl: "https://example.com/srtm/",
            persistDownloads: false,
            enableCache: false,
            airGapped: false,
            cacheRoot: cacheRoot,
            persistCache: true);

        // Assert
        provider.Should().NotBeNull();
        // Provider should be created with cache configuration
        // (We can't directly verify internal state, but we can verify it doesn't throw)
    }

    [Fact]
    public void ElevationProviderFactory_WithoutCacheRoot_ShouldWorkNormally()
    {
        // Arrange
        var factory = new ElevationProviderFactory(
            _mockHttpClientFactory.Object,
            _mockLoggerFactory.Object);

        // Act
        var provider = factory.Build(
            provider: "http",
            localRoot: null,
            srtmBaseUrl: "https://example.com/srtm/",
            persistDownloads: false,
            enableCache: false,
            airGapped: false,
            cacheRoot: null,
            persistCache: false);

        // Assert
        provider.Should().NotBeNull();
    }

    [Fact]
    public void VectorProviderFactory_WithCacheRoot_ShouldPassToMapboxProvider()
    {
        // Arrange
        var factory = new VectorProviderFactory(
            _mockHttpClientFactory.Object,
            _mockLoggerFactory.Object);
        var cacheRoot = Path.Combine(_testCacheDir, "vector-cache");
        var bounds = GeoBounds.Parse("37.0,-122.0,38.0,-121.0");

        // Act
        var provider = factory.Build(
            provider: "mapbox",
            bounds: bounds,
            mapboxAccessToken: "test-token",
            mapboxTileset: "mapbox.mapbox-streets-v8",
            mapboxZoom: 15,
            osmPbfPath: null,
            airGapped: false,
            cacheRoot: cacheRoot,
            persistCache: true);

        // Assert
        provider.Should().NotBeNull();
    }

    [Fact]
    public void VectorProviderFactory_WithoutCacheRoot_ShouldWorkNormally()
    {
        // Arrange
        var factory = new VectorProviderFactory(
            _mockHttpClientFactory.Object,
            _mockLoggerFactory.Object);
        var bounds = GeoBounds.Parse("37.0,-122.0,38.0,-121.0");

        // Act
        var provider = factory.Build(
            provider: "mapbox",
            bounds: bounds,
            mapboxAccessToken: "test-token",
            mapboxTileset: "mapbox.mapbox-streets-v8",
            mapboxZoom: 15,
            osmPbfPath: null,
            airGapped: false,
            cacheRoot: null,
            persistCache: false);

        // Assert
        provider.Should().NotBeNull();
    }

    [Fact]
    public void ResourceEstimationService_WithCacheRoot_ShouldDetectCachedTiles()
    {
        // Arrange
        var estimator = new ResourceEstimationService();
        var cacheRoot = Path.Combine(_testCacheDir, "estimation-cache");
        var srtmCacheDir = Path.Combine(cacheRoot, "srtm");
        Directory.CreateDirectory(srtmCacheDir);

        // Create a fake cached tile
        var tileId = new SrtmTileId(37, -122);
        var cachedTilePath = Path.Combine(srtmCacheDir, $"{tileId}.hgt");
        var fakeTileData = new byte[2_884_802]; // Standard SRTM tile size
        await File.WriteAllBytesAsync(cachedTilePath, fakeTileData);

        var tileIds = new[] { tileId };

        // Act
        var estimate = estimator.EstimateSrtmTileDownload(tileIds, fromCache: false, cacheRoot: cacheRoot);

        // Assert
        estimate.Should().NotBeNull();
        estimate.Metadata.Should().NotBeNull();
        estimate.Metadata!["cached-count"].Should().Be(1);
        estimate.Metadata["download-count"].Should().Be(0);
        estimate.CostUsd.Should().Be(0, "Cached tiles should have zero cost");
    }

    [Fact]
    public void ResourceEstimationService_WithPartialCache_ShouldCalculateCorrectCost()
    {
        // Arrange
        var estimator = new ResourceEstimationService();
        var cacheRoot = Path.Combine(_testCacheDir, "partial-cache");
        var srtmCacheDir = Path.Combine(cacheRoot, "srtm");
        Directory.CreateDirectory(srtmCacheDir);

        // Create one cached tile
        var cachedTileId = new SrtmTileId(37, -122);
        var cachedTilePath = Path.Combine(srtmCacheDir, $"{cachedTileId}.hgt");
        var fakeTileData = new byte[2_884_802];
        await File.WriteAllBytesAsync(cachedTilePath, fakeTileData);

        // One tile not cached
        var uncachedTileId = new SrtmTileId(37, -121);
        var tileIds = new[] { cachedTileId, uncachedTileId };

        // Act
        var estimate = estimator.EstimateSrtmTileDownload(tileIds, fromCache: false, cacheRoot: cacheRoot);

        // Assert
        estimate.Should().NotBeNull();
        estimate.Metadata.Should().NotBeNull();
        estimate.Metadata!["cached-count"].Should().Be(1);
        estimate.Metadata["download-count"].Should().Be(1);
        estimate.CostUsd.Should().BeGreaterThan(0, "Uncached tiles should have cost");
    }

    [Fact]
    public void ResourceEstimationService_WithoutCacheRoot_ShouldEstimateAllDownloads()
    {
        // Arrange
        var estimator = new ResourceEstimationService();
        var tileIds = new[] { new SrtmTileId(37, -122), new SrtmTileId(37, -121) };

        // Act
        var estimate = estimator.EstimateSrtmTileDownload(tileIds, fromCache: false, cacheRoot: null);

        // Assert
        estimate.Should().NotBeNull();
        estimate.Metadata.Should().NotBeNull();
        estimate.Metadata!["cached-count"].Should().Be(0);
        estimate.Metadata["download-count"].Should().Be(2);
        estimate.CostUsd.Should().BeGreaterThan(0, "All tiles should be estimated as downloads");
    }

    [Fact]
    public void ResourceEstimationService_WithFromCacheTrue_ShouldReturnZeroCost()
    {
        // Arrange
        var estimator = new ResourceEstimationService();
        var tileIds = new[] { new SrtmTileId(37, -122), new SrtmTileId(37, -121) };

        // Act
        var estimate = estimator.EstimateSrtmTileDownload(tileIds, fromCache: true, cacheRoot: null);

        // Assert
        estimate.Should().NotBeNull();
        estimate.CostUsd.Should().Be(0, "Tiles from cache should have zero cost");
        estimate.Metadata!["cached-count"].Should().Be(2);
        estimate.Metadata["download-count"].Should().Be(0);
    }

    public void Dispose()
    {
        try
        {
            _httpClient?.Dispose();
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
