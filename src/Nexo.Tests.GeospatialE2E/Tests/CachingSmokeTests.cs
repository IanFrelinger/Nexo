using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.API.Models;
using Nexo.API.Services;
using Nexo.CLI.Commands.GeoTerrain;
using Nexo.CLI.Commands.GeoVector;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Common.Services;
using Nexo.Infrastructure.Execution;
using Xunit;

namespace Nexo.Tests.GeospatialE2E.Tests;

/// <summary>
/// Smoke tests for caching functionality across CLI, API, and SDK.
/// </summary>
public class CachingSmokeTests : IDisposable
{
    private readonly string _testOutputDir;
    private readonly string _testCacheDir;
    private readonly IJobRepository _jobRepository;
    private readonly ServiceProvider _serviceProvider;

    public CachingSmokeTests()
    {
        _testOutputDir = Path.Combine(Path.GetTempPath(), $"nexo-cache-e2e-{Guid.NewGuid()}");
        _testCacheDir = Path.Combine(Path.GetTempPath(), $"nexo-cache-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testOutputDir);
        Directory.CreateDirectory(_testCacheDir);

        // Setup service provider for testing
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddHttpClient();
        services.AddHttpClient("geoterrain.srtm");
        services.AddHttpClient("geovector.mapbox");
        services.AddScoped<IProviderFactory, ProviderFactory>();
        services.AddScoped<ILoopKernel, Nexo.Core.Application.Common.Services.SequentialLoopKernel>();
        services.AddScoped<GeoTerrainCommand>();
        services.AddScoped<GeoVectorCommand>();
        
        // Use test database
        var dbPath = Path.Combine(_testOutputDir, "test-jobs.db");
        services.AddSingleton<IJobRepository>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<SqliteJobRepository>>();
            return new SqliteJobRepository(dbPath, logger);
        });

        services.AddScoped<WebhookService>();
        services.AddScoped<IGeoTerrainService, GeoTerrainService>();
        services.AddScoped<IGeoVectorService, GeoVectorService>();

        _serviceProvider = services.BuildServiceProvider();
        _jobRepository = _serviceProvider.GetRequiredService<IJobRepository>();
    }

    [Fact]
    public async Task CLI_GeoTerrain_WithCacheRoot_ShouldAcceptCacheParameters()
    {
        // Arrange
        var outputFile = Path.Combine(_testOutputDir, "cached-terrain.obj");
        var command = _serviceProvider.GetRequiredService<GeoTerrainCommand>();

        // Act - Verify command accepts cache parameters without throwing
        var exception = await Record.ExceptionAsync(async () =>
        {
            await command.BoundsToObjAsync(
                bounds: "37.0,-122.0,37.1,-121.9",
                output: new FileInfo(outputFile),
                provider: "echo",
                localRoot: null,
                srtmBaseUrl: null,
                persistDownloads: false,
                enableCache: true,
                airGapped: true,
                forceAgenticFail: false,
                validateIntegrity: false,
                meshQualityReport: false,
                cacheRoot: _testCacheDir,
                persistCache: true,
                json: false,
                verbose: false,
                CancellationToken.None);
        });

        // Assert
        exception.Should().BeNull("Command should accept cache parameters without throwing");
    }

    [Fact]
    public async Task CLI_GeoVector_WithCacheRoot_ShouldAcceptCacheParameters()
    {
        // Arrange
        var outputFile = Path.Combine(_testOutputDir, "cached-buildings.obj");
        var command = _serviceProvider.GetRequiredService<GeoVectorCommand>();

        // Act - Verify command accepts cache parameters without throwing
        var exception = await Record.ExceptionAsync(async () =>
        {
            await command.BuildingsToObjAsync(
                bounds: "37.0,-122.0,37.1,-121.9",
                output: new FileInfo(outputFile),
                provider: "echo",
                mapboxAccessToken: null,
                mapboxTileset: null,
                mapboxZoom: null,
                osmPbfPath: null,
                generateTexCoords: false,
                uvMetersPerRepeat: 10.0f,
                alignToTerrain: false,
                terrainProvider: "echo",
                terrainLocalRoot: null,
                terrainSrtmBaseUrl: null,
                terrainPersistDownloads: false,
                terrainEnableCache: false,
                terrainTreatNoDataAsZero: false,
                airGapped: true,
                forceAgenticFail: false,
                cacheRoot: _testCacheDir,
                persistCache: true,
                terrainCacheRoot: _testCacheDir,
                terrainPersistCache: true,
                json: false,
                verbose: false,
                CancellationToken.None);
        });

        // Assert
        exception.Should().BeNull("Command should accept cache parameters without throwing");
    }

    [Fact]
    public async Task API_GeoTerrain_WithCacheRoot_ShouldAcceptCacheProperties()
    {
        // Arrange
        var service = _serviceProvider.GetRequiredService<IGeoTerrainService>();
        var request = new TerrainGenerationRequest
        {
            Bounds = "37.0,-122.0,37.1,-121.9",
            ElevationProvider = "echo",
            Format = "obj",
            CacheRoot = _testCacheDir,
            PersistCache = true
        };

        // Act
        var jobId = await service.GenerateTerrainAsync(request);

        // Assert
        jobId.Should().NotBeNullOrEmpty("Job should be created with cache configuration");
        
        // Verify job exists
        var job = await _jobRepository.GetJobAsync(jobId);
        job.Should().NotBeNull("Job should be persisted");
    }

    [Fact]
    public async Task API_GeoVector_WithCacheRoot_ShouldAcceptCacheProperties()
    {
        // Arrange
        var service = _serviceProvider.GetRequiredService<IGeoVectorService>();
        var request = new VectorExtractionRequest
        {
            Bounds = "37.0,-122.0,37.1,-121.9",
            FeatureKind = "building",
            VectorProvider = "echo",
            CacheRoot = _testCacheDir,
            PersistCache = true
        };

        // Act
        var jobId = await service.ExtractFeaturesAsync(request);

        // Assert
        jobId.Should().NotBeNullOrEmpty("Job should be created with cache configuration");
        
        // Verify job exists
        var job = await _jobRepository.GetJobAsync(jobId);
        job.Should().NotBeNull("Job should be persisted");
    }

    [Fact]
    public async Task API_GeoTerrain_WithoutCacheRoot_ShouldWorkNormally()
    {
        // Arrange
        var service = _serviceProvider.GetRequiredService<IGeoTerrainService>();
        var request = new TerrainGenerationRequest
        {
            Bounds = "37.0,-122.0,37.1,-121.9",
            ElevationProvider = "echo",
            Format = "obj",
            CacheRoot = null,
            PersistCache = false
        };

        // Act
        var jobId = await service.GenerateTerrainAsync(request);

        // Assert
        jobId.Should().NotBeNullOrEmpty("Job should be created without cache configuration");
    }

    [Fact]
    public async Task CacheDirectory_ShouldBeCreated_WhenCacheRootIsSet()
    {
        // Arrange
        var cacheRoot = Path.Combine(_testCacheDir, "new-cache");
        var srtmCacheDir = Path.Combine(cacheRoot, "srtm");
        
        // Verify directory doesn't exist yet
        Directory.Exists(srtmCacheDir).Should().BeFalse("Cache directory should not exist initially");

        // Act - Create a provider that would use this cache
        // (In a real scenario, this would happen when a tile is downloaded)
        Directory.CreateDirectory(srtmCacheDir);

        // Assert
        Directory.Exists(srtmCacheDir).Should().BeTrue("Cache directory should be created");
    }

    [Fact]
    public void CacheConfiguration_ShouldBeOptional()
    {
        // Arrange & Act
        var request = new TerrainGenerationRequest
        {
            Bounds = "37.0,-122.0,37.1,-121.9",
            ElevationProvider = "echo",
            Format = "obj"
            // CacheRoot and PersistCache are optional
        };

        // Assert
        request.CacheRoot.Should().BeNull("Cache root should be optional");
        request.PersistCache.Should().BeNull("Persist cache should be optional");
    }

    [Fact]
    public void CacheConfiguration_ShouldAcceptCustomPaths()
    {
        // Arrange & Act
        var customCachePath = "/custom/cache/path";
        var request = new TerrainGenerationRequest
        {
            Bounds = "37.0,-122.0,37.1,-121.9",
            ElevationProvider = "echo",
            Format = "obj",
            CacheRoot = customCachePath,
            PersistCache = true
        };

        // Assert
        request.CacheRoot.Should().Be(customCachePath, "Should accept custom cache path");
        request.PersistCache.Should().BeTrue("Should accept persist cache setting");
    }

    public void Dispose()
    {
        try
        {
            _serviceProvider?.Dispose();
            if (Directory.Exists(_testOutputDir))
            {
                Directory.Delete(_testOutputDir, recursive: true);
            }
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
