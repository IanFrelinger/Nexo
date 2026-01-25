using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.API.Models;
using Nexo.API.Services;
using Nexo.CLI.Commands.World;
using Xunit;

namespace Nexo.Tests.GeospatialUnit.Tests;

public class WorldServiceTests : IDisposable
{
    private readonly Mock<IWorldCommand> _mockCommand;
    private readonly Mock<ILogger<WorldService>> _mockLogger;
    private readonly Mock<IJobRepository> _mockRepository;
    private readonly WorldService _service;
    private readonly string _testOutputDir;

    public WorldServiceTests()
    {
        _testOutputDir = Path.Combine(Path.GetTempPath(), $"nexo-unit-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testOutputDir);

        _mockCommand = new Mock<IWorldCommand>();
        _mockLogger = new Mock<ILogger<WorldService>>();
        _mockRepository = new Mock<IJobRepository>();

        _service = new WorldService(
            _mockCommand.Object,
            _mockLogger.Object,
            _mockRepository.Object,
            null); // WebhookService is optional
    }

    [Fact]
    public async Task GenerateWorldAsync_ShouldCreateJob_AndReturnJobId()
    {
        // Arrange
        var request = new WorldGenerationRequest
        {
            Bounds = "37.0,-122.0,37.1,-121.9",
            ElevationProvider = "srtm",
            VectorProvider = "hybrid",
            Format = "obj"
        };

        _mockCommand
            .Setup(c => c.BuildAsync(
                It.IsAny<string>(), // bounds
                It.IsAny<DirectoryInfo>(), // outDir
                It.IsAny<string>(), // terrainElevationProvider
                It.IsAny<string?>(), // terrainLocalRoot
                It.IsAny<string?>(), // terrainSrtmBaseUrl
                It.IsAny<bool>(), // terrainPersistDownloads
                It.IsAny<bool>(), // terrainEnableCache
                It.IsAny<string>(), // vectorProvider
                It.IsAny<string?>(), // osmPbfPath
                It.IsAny<string?>(), // mapboxAccessToken
                It.IsAny<string?>(), // mapboxTileset
                It.IsAny<int?>(), // mapboxZoom
                It.IsAny<int>(), // terrainChunkSamples
                It.IsAny<string?>(), // terrainLodFactors
                It.IsAny<string?>(), // lodTriBudgets
                It.IsAny<int>(), // instancesChunkSamples
                It.IsAny<bool>(), // enableTerrainImagery
                It.IsAny<string?>(), // terrainImageryTileset
                It.IsAny<string?>(), // terrainImageryFormat
                It.IsAny<int?>(), // terrainImageryZoom
                It.IsAny<bool>(), // waterFlattenToTerrain
                It.IsAny<string>(), // meshFormat
                It.IsAny<string>(), // projection
                It.IsAny<bool>(), // generateVectorTextures
                It.IsAny<bool>(), // airGapped
                It.IsAny<bool>(), // json
                It.IsAny<bool>(), // verbose
                It.IsAny<CancellationToken>())) // ct
            .ReturnsAsync(0);

        _mockRepository
            .Setup(r => r.CreateJobAsync(It.IsAny<JobStatusResponse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JobStatusResponse job, CancellationToken ct) => job.JobId);

        // Act
        var jobId = await _service.GenerateWorldAsync(request);

        // Assert
        jobId.Should().NotBeNullOrEmpty();
        _mockRepository.Verify(r => r.CreateJobAsync(
            It.Is<JobStatusResponse>(j => j.Status == "pending" && j.Progress == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateWorldAsync_ShouldReturnInvalid_WhenDirectoryDoesNotExist()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testOutputDir, "nonexistent");

        // Act
        var result = await _service.ValidateWorldAsync(nonExistentPath);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Issues.Should().Contain(i => i.Contains("does not exist"));
    }

    [Fact]
    public async Task ValidateWorldAsync_ShouldCallCommandValidate()
    {
        // Arrange
        var bundlePath = Path.Combine(_testOutputDir, "bundle");
        Directory.CreateDirectory(bundlePath);

        _mockCommand
            .Setup(c => c.ValidateAsync(
                It.IsAny<DirectoryInfo>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _service.ValidateWorldAsync(bundlePath);

        // Assert
        _mockCommand.Verify(c => c.ValidateAsync(
            It.Is<DirectoryInfo>(d => d.FullName == bundlePath),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Once);
        
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetJobStatusAsync_ShouldReturnJob_FromRepository()
    {
        // Arrange
        var jobId = Guid.NewGuid().ToString("N");
        var expectedJob = new JobStatusResponse
        {
            JobId = jobId,
            Status = "completed",
            Progress = 100,
            CreatedAt = DateTime.UtcNow
        };

        _mockRepository
            .Setup(r => r.GetJobAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedJob);

        // Act
        var result = await _service.GetJobStatusAsync(jobId);

        // Assert
        result.Should().NotBeNull();
        result!.JobId.Should().Be(jobId);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testOutputDir))
            {
                Directory.Delete(_testOutputDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
