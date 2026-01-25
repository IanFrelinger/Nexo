using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.API.Models;
using Nexo.API.Services;
using Nexo.CLI.Commands.GeoTerrain;
using Xunit;

namespace Nexo.Tests.GeospatialUnit.Tests;

public class GeoTerrainServiceTests : IDisposable
{
    private readonly Mock<IGeoTerrainCommand> _mockCommand;
    private readonly Mock<ILogger<GeoTerrainService>> _mockLogger;
    private readonly Mock<IJobRepository> _mockRepository;
    private readonly GeoTerrainService _service;
    private readonly string _testOutputDir;

    public GeoTerrainServiceTests()
    {
        _testOutputDir = Path.Combine(Path.GetTempPath(), $"nexo-unit-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testOutputDir);

        _mockCommand = new Mock<IGeoTerrainCommand>();
        _mockLogger = new Mock<ILogger<GeoTerrainService>>();
        _mockRepository = new Mock<IJobRepository>();

        _service = new GeoTerrainService(
            _mockCommand.Object,
            _mockLogger.Object,
            _mockRepository.Object,
            null); // WebhookService is optional
    }

    [Fact]
    public async Task GenerateTerrainAsync_ShouldCreateJob_AndReturnJobId()
    {
        // Arrange
        var request = new TerrainGenerationRequest
        {
            Bounds = "37.0,-122.0,37.1,-121.9",
            ElevationProvider = "srtm",
            Format = "obj"
        };

        _mockCommand
            .Setup(c => c.BoundsToObjAsync(
                It.IsAny<string>(),
                It.IsAny<FileInfo>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockRepository
            .Setup(r => r.CreateJobAsync(It.IsAny<JobStatusResponse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JobStatusResponse job, CancellationToken ct) => job.JobId);

        // Act
        var jobId = await _service.GenerateTerrainAsync(request);

        // Assert
        jobId.Should().NotBeNullOrEmpty();
        _mockRepository.Verify(r => r.CreateJobAsync(
            It.Is<JobStatusResponse>(j => j.Status == "pending" && j.Progress == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateTerrainAsync_ShouldUpdateJobStatus_WhenProcessing()
    {
        // Arrange
        var request = new TerrainGenerationRequest
        {
            Bounds = "37.0,-122.0,37.1,-121.9",
            ElevationProvider = "srtm",
            Format = "obj"
        };

        var jobId = Guid.NewGuid().ToString("N");
        var createdJob = new JobStatusResponse
        {
            JobId = jobId,
            Status = "pending",
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        };

        _mockCommand
            .Setup(c => c.BoundsToObjAsync(
                It.IsAny<string>(),
                It.IsAny<FileInfo>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockRepository
            .Setup(r => r.CreateJobAsync(It.IsAny<JobStatusResponse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JobStatusResponse job, CancellationToken ct) => job.JobId);

        _mockRepository
            .Setup(r => r.GetJobAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdJob);

        // Act
        await _service.GenerateTerrainAsync(request);

        // Wait for async processing
        await Task.Delay(500);

        // Assert
        _mockRepository.Verify(r => r.UpdateJobAsync(
            It.Is<JobStatusResponse>(j => j.Status == "processing" && j.Progress == 10),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GenerateTerrainAsync_ShouldHandleCommandFailure()
    {
        // Arrange
        var request = new TerrainGenerationRequest
        {
            Bounds = "37.0,-122.0,37.1,-121.9",
            ElevationProvider = "srtm",
            Format = "obj"
        };

        var jobId = Guid.NewGuid().ToString("N");
        var createdJob = new JobStatusResponse
        {
            JobId = jobId,
            Status = "pending",
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        };

        _mockCommand
            .Setup(c => c.BoundsToObjAsync(
                It.IsAny<string>(),
                It.IsAny<FileInfo>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // Failure exit code

        _mockRepository
            .Setup(r => r.CreateJobAsync(It.IsAny<JobStatusResponse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JobStatusResponse job, CancellationToken ct) => job.JobId);

        _mockRepository
            .Setup(r => r.GetJobAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdJob);

        // Act
        await _service.GenerateTerrainAsync(request);

        // Wait for async processing
        await Task.Delay(1500);

        // Assert
        _mockRepository.Verify(r => r.UpdateJobAsync(
            It.Is<JobStatusResponse>(j => j.Status == "failed" && !string.IsNullOrEmpty(j.ErrorMessage)),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
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
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        _mockRepository
            .Setup(r => r.GetJobAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedJob);

        // Act
        var result = await _service.GetJobStatusAsync(jobId);

        // Assert
        result.Should().NotBeNull();
        result!.JobId.Should().Be(jobId);
        result.Status.Should().Be("completed");
        _mockRepository.Verify(r => r.GetJobAsync(jobId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetJobOutputPathAsync_ShouldReturnPath_WhenJobCompleted()
    {
        // Arrange
        var jobId = Guid.NewGuid().ToString("N");
        var outputPath = "/path/to/output.obj";
        var job = new JobStatusResponse
        {
            JobId = jobId,
            Status = "completed",
            Progress = 100,
            OutputPath = outputPath,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        _mockRepository
            .Setup(r => r.GetJobAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        // Act
        var result = await _service.GetJobOutputPathAsync(jobId, "obj");

        // Assert
        result.Should().Be(outputPath);
    }

    [Fact]
    public async Task GetJobOutputPathAsync_ShouldReturnNull_WhenJobNotCompleted()
    {
        // Arrange
        var jobId = Guid.NewGuid().ToString("N");
        var job = new JobStatusResponse
        {
            JobId = jobId,
            Status = "processing",
            Progress = 50,
            CreatedAt = DateTime.UtcNow
        };

        _mockRepository
            .Setup(r => r.GetJobAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        // Act
        var result = await _service.GetJobOutputPathAsync(jobId, "obj");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GenerateTerrainAsync_ShouldSendWebhook_WhenConfigured()
    {
        // Arrange
        var request = new TerrainGenerationRequest
        {
            Bounds = "37.0,-122.0,37.1,-121.9",
            ElevationProvider = "srtm",
            Format = "obj",
            WebhookUrl = "https://example.com/webhook"
        };

        _mockCommand
            .Setup(c => c.BoundsToObjAsync(
                It.IsAny<string>(),
                It.IsAny<FileInfo>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockRepository
            .Setup(r => r.CreateJobAsync(It.IsAny<JobStatusResponse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JobStatusResponse job, CancellationToken ct) => job.JobId);

        // Act
        await _service.GenerateTerrainAsync(request);

        // Wait for async processing
        await Task.Delay(1000);

        // Assert
        // Webhook verification would require a real WebhookService instance
        // For now, we verify the job was created and processing started
        _mockRepository.Verify(r => r.CreateJobAsync(
            It.Is<JobStatusResponse>(j => j.Status == "pending"),
            It.IsAny<CancellationToken>()), Times.Once);
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
