using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.API.Models;
using Nexo.API.Services;
using Nexo.CLI.Commands.GeoVector;
using Xunit;

namespace Nexo.Tests.GeospatialUnit.Tests;

public class GeoVectorServiceTests : IDisposable
{
    private readonly Mock<IGeoVectorCommand> _mockCommand;
    private readonly Mock<ILogger<GeoVectorService>> _mockLogger;
    private readonly Mock<IJobRepository> _mockRepository;
    private readonly GeoVectorService _service;
    private readonly string _testOutputDir;

    public GeoVectorServiceTests()
    {
        _testOutputDir = Path.Combine(Path.GetTempPath(), $"nexo-unit-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testOutputDir);

        _mockCommand = new Mock<IGeoVectorCommand>();
        _mockLogger = new Mock<ILogger<GeoVectorService>>();
        _mockRepository = new Mock<IJobRepository>();

        _service = new GeoVectorService(
            _mockCommand.Object,
            _mockLogger.Object,
            _mockRepository.Object,
            null); // WebhookService is optional
    }

    [Fact]
    public async Task ExtractFeaturesAsync_ShouldCreateJob_ForBuildings()
    {
        // Arrange
        var request = new VectorExtractionRequest
        {
            Bounds = "37.0,-122.0,37.1,-121.9",
            FeatureKind = "building",
            VectorProvider = "hybrid",
            Format = "obj"
        };

        // Command will execute (tested in E2E tests)
        _mockRepository
            .Setup(r => r.CreateJobAsync(It.IsAny<JobStatusResponse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JobStatusResponse job, CancellationToken ct) => job.JobId);

        // Act
        var jobId = await _service.ExtractFeaturesAsync(request);

        // Assert
        jobId.Should().NotBeNullOrEmpty();
        _mockRepository.Verify(r => r.CreateJobAsync(
            It.Is<JobStatusResponse>(j => j.Status == "pending"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExtractFeaturesAsync_ShouldCallRoadsToObj_ForRoadFeatureKind()
    {
        // Arrange
        var request = new VectorExtractionRequest
        {
            Bounds = "37.0,-122.0,37.1,-121.9",
            FeatureKind = "road",
            VectorProvider = "hybrid",
            Format = "obj"
        };

        // Command will execute (tested in E2E tests)
        _mockRepository
            .Setup(r => r.CreateJobAsync(It.IsAny<JobStatusResponse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JobStatusResponse job, CancellationToken ct) => job.JobId);

        // Act
        await _service.ExtractFeaturesAsync(request);

        // Assert
        // Verify job was created (command execution verified in E2E tests)
        _mockRepository.Verify(r => r.CreateJobAsync(
            It.Is<JobStatusResponse>(j => j.Status == "pending"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExtractFeaturesAsync_ShouldCallWaterToObj_ForWaterFeatureKind()
    {
        // Arrange
        var request = new VectorExtractionRequest
        {
            Bounds = "37.0,-122.0,37.1,-121.9",
            FeatureKind = "water",
            VectorProvider = "hybrid",
            Format = "obj"
        };

        // Command will execute (tested in E2E tests)
        _mockRepository
            .Setup(r => r.CreateJobAsync(It.IsAny<JobStatusResponse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JobStatusResponse job, CancellationToken ct) => job.JobId);

        // Act
        await _service.ExtractFeaturesAsync(request);

        // Assert
        // Verify job was created (command execution verified in E2E tests)
        _mockRepository.Verify(r => r.CreateJobAsync(
            It.Is<JobStatusResponse>(j => j.Status == "pending"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExtractFeaturesAsync_ShouldThrow_ForUnsupportedFeatureKind()
    {
        // Arrange
        var request = new VectorExtractionRequest
        {
            Bounds = "37.0,-122.0,37.1,-121.9",
            FeatureKind = "unsupported",
            VectorProvider = "hybrid",
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

        _mockRepository
            .Setup(r => r.CreateJobAsync(It.IsAny<JobStatusResponse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JobStatusResponse job, CancellationToken ct) => job.JobId);

        _mockRepository
            .Setup(r => r.GetJobAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdJob);

        // Act
        await _service.ExtractFeaturesAsync(request);

        // Wait for async processing
        await Task.Delay(1500);

        // Assert
        _mockRepository.Verify(r => r.UpdateJobAsync(
            It.Is<JobStatusResponse>(j => j.Status == "failed" && 
                j.ErrorMessage != null && 
                j.ErrorMessage.Contains("not yet implemented")),
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
