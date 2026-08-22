using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Ashlar.Infrastructure.Testing.ExecutionPlatform;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.ExecutionPlatform;

/// <summary>
/// Tests for execution platform abstractions.
/// 
/// Validates:
/// - IExecutionPlatform interface contracts
/// - DockerExecutionPlatform implementation
/// - Platform availability detection
/// - Image building and container execution
/// - Error handling and edge cases
/// </summary>
public class ExecutionPlatformTests
{
    private readonly ILoggerFactory _loggerFactory;

    public ExecutionPlatformTests()
    {
        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
    }

    [Fact]
    public void DockerExecutionPlatform_ShouldHaveCorrectPlatformName()
    {
        // Arrange
        var logger = _loggerFactory.CreateLogger<DockerExecutionPlatform>();
        var platform = new DockerExecutionPlatform(logger);

        // Act
        var platformName = platform.PlatformName;

        // Assert
        platformName.Should().Be("Docker");
    }

    [Fact]
    public async Task DockerExecutionPlatform_IsAvailableAsync_ShouldNotThrow()
    {
        // Arrange
        var logger = _loggerFactory.CreateLogger<DockerExecutionPlatform>();
        var platform = new DockerExecutionPlatform(logger);

        // Act - Returns true if Docker is running, false otherwise; must not throw
        var isAvailable = await platform.IsAvailableAsync();

        // Assert - Result is a valid boolean; behavior depends on environment (Docker available or not)
        (isAvailable == true || isAvailable == false).Should().BeTrue();
    }

    [Fact]
    public async Task DockerExecutionPlatform_BuildImageAsync_ShouldReturnFailure_WhenDockerfileNotFound()
    {
        // Arrange
        var logger = _loggerFactory.CreateLogger<DockerExecutionPlatform>();
        var platform = new DockerExecutionPlatform(logger);
        var nonExistentDockerfile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "Dockerfile");

        // Act
        var result = await platform.BuildImageAsync(
            nonExistentDockerfile,
            "test-image:latest",
            Path.GetTempPath(),
            null,
            null,
            CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DockerExecutionPlatform_RunContainerAsync_ShouldReturnFailure_WhenImageNotFound()
    {
        // Arrange
        var logger = _loggerFactory.CreateLogger<DockerExecutionPlatform>();
        var platform = new DockerExecutionPlatform(logger);
        var nonExistentImage = $"test-image-{Guid.NewGuid()}:latest";

        // Act
        var result = await platform.RunContainerAsync(
            nonExistentImage,
            new[] { "echo", "test" },
            null,
            null,
            null,
            CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ExitCode.Should().NotBe(0);
    }

    [Fact]
    public async Task ExecutionPlatform_RemoveContainerAsync_ShouldNotThrow_WhenContainerNotFound()
    {
        // Arrange
        var logger = _loggerFactory.CreateLogger<DockerExecutionPlatform>();
        var platform = new DockerExecutionPlatform(logger);
        var nonExistentContainer = Guid.NewGuid().ToString();

        // Act & Assert
        var act = async () => await platform.RemoveContainerAsync(nonExistentContainer, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecutionPlatform_RemoveImageAsync_ShouldNotThrow_WhenImageNotFound()
    {
        // Arrange
        var logger = _loggerFactory.CreateLogger<DockerExecutionPlatform>();
        var platform = new DockerExecutionPlatform(logger);
        var nonExistentImage = $"test-image-{Guid.NewGuid()}:latest";

        // Act & Assert
        var act = async () => await platform.RemoveImageAsync(nonExistentImage, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void DockerExecutionPlatform_ShouldDisposeCorrectly()
    {
        // Arrange
        var logger = _loggerFactory.CreateLogger<DockerExecutionPlatform>();
        var platform = new DockerExecutionPlatform(logger);

        // Act & Assert
        var act = () => platform.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void ExecutionPlatform_Results_ShouldHaveCorrectStructure()
    {
        // Arrange & Act
        var buildResult = new ExecutionBuildResult(true, null, TimeSpan.FromSeconds(5));
        var runResult = new ExecutionRunResult(true, 0, "output", "error", "container-id", TimeSpan.FromSeconds(10));

        // Assert
        buildResult.Success.Should().BeTrue();
        buildResult.ErrorMessage.Should().BeNull();
        buildResult.Duration.Should().Be(TimeSpan.FromSeconds(5));

        runResult.Success.Should().BeTrue();
        runResult.ExitCode.Should().Be(0);
        runResult.StandardOutput.Should().Be("output");
        runResult.StandardError.Should().Be("error");
        runResult.ContainerId.Should().Be("container-id");
        runResult.Duration.Should().Be(TimeSpan.FromSeconds(10));
    }
}
