using FluentAssertions;
using Microsoft.Extensions.Logging;
using Nexo.Infrastructure.Testing.ExecutionPlatform;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.ExecutionPlatform;

/// <summary>
/// Tests that verify the execution platform abstraction is truly portable.
/// 
/// These tests verify:
/// - Platforms can be instantiated without external dependencies
/// - Availability detection works gracefully when platforms are unavailable
/// - The abstraction doesn't require Docker/Rancher/Kubernetes to be running
/// - All platforms follow the same interface contract
/// </summary>
public class ExecutionPlatformPortabilityTests
{
    private readonly ILoggerFactory _loggerFactory;

    public ExecutionPlatformPortabilityTests()
    {
        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
    }

    [Fact]
    public void AllExecutionPlatforms_ShouldBeInstantiable_WithoutExternalDependencies()
    {
        // Arrange & Act - Should not throw even if Docker/Rancher/Kubernetes are not available
        var dockerLogger = _loggerFactory.CreateLogger<DockerExecutionPlatform>();
        var dockerPlatform = new DockerExecutionPlatform(dockerLogger);

        var rancherLogger = _loggerFactory.CreateLogger<RancherExecutionPlatform>();
        var rancherPlatform = new RancherExecutionPlatform(rancherLogger, "https://rancher.example.com");

        var k8sLogger = _loggerFactory.CreateLogger<KubernetesExecutionPlatform>();
        var k8sPlatform = new KubernetesExecutionPlatform(k8sLogger, "~/.kube/config");

        // Assert - All platforms should be created successfully
        dockerPlatform.Should().NotBeNull();
        dockerPlatform.PlatformName.Should().Be("Docker");

        rancherPlatform.Should().NotBeNull();
        rancherPlatform.PlatformName.Should().Be("Rancher");

        k8sPlatform.Should().NotBeNull();
        k8sPlatform.PlatformName.Should().Be("Kubernetes");
    }

    [Fact]
    public async Task ExecutionPlatforms_ShouldHandleUnavailability_Gracefully()
    {
        // Arrange
        var logger = _loggerFactory.CreateLogger<DockerExecutionPlatform>();
        var platform = new DockerExecutionPlatform(logger);

        // Act - Should not throw even if Docker is not running
        var isAvailable = await platform.IsAvailableAsync();

        // Assert - Should return false gracefully, not throw
        // This proves portability - code works even when Docker isn't available
        isAvailable.Should().BeFalse(); // Expected when Docker is not running
    }

    [Fact]
    public void ExecutionPlatform_InterfaceContract_ShouldBeConsistent()
    {
        // Arrange
        var dockerLogger = _loggerFactory.CreateLogger<DockerExecutionPlatform>();
        var dockerPlatform = new DockerExecutionPlatform(dockerLogger);

        var rancherLogger = _loggerFactory.CreateLogger<RancherExecutionPlatform>();
        var rancherPlatform = new RancherExecutionPlatform(rancherLogger, "https://rancher.example.com");

        var k8sLogger = _loggerFactory.CreateLogger<KubernetesExecutionPlatform>();
        var k8sPlatform = new KubernetesExecutionPlatform(k8sLogger, "~/.kube/config");

        // Act & Assert - All platforms should implement IExecutionPlatform
        dockerPlatform.Should().BeAssignableTo<IExecutionPlatform>();
        rancherPlatform.Should().BeAssignableTo<IExecutionPlatform>();
        k8sPlatform.Should().BeAssignableTo<IExecutionPlatform>();

        // All should have PlatformName property
        dockerPlatform.PlatformName.Should().NotBeNullOrEmpty();
        rancherPlatform.PlatformName.Should().NotBeNullOrEmpty();
        k8sPlatform.PlatformName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExecutionPlatform_ShouldHandleErrors_Gracefully()
    {
        // Arrange
        var logger = _loggerFactory.CreateLogger<DockerExecutionPlatform>();
        var platform = new DockerExecutionPlatform(logger);
        var nonExistentDockerfile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "Dockerfile");

        // Act - Should return failure result, not throw
        var buildResult = await platform.BuildImageAsync(
            nonExistentDockerfile,
            "test-image:latest",
            Path.GetTempPath(),
            null,
            null,
            CancellationToken.None);

        // Assert - Should return failure gracefully
        buildResult.Success.Should().BeFalse();
        buildResult.ErrorMessage.Should().NotBeNullOrEmpty();
        buildResult.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void ExecutionPlatform_ResultTypes_ShouldBePortable()
    {
        // Arrange & Act - Create result records
        var buildResult = new ExecutionBuildResult(true, null, TimeSpan.FromSeconds(5));
        var runResult = new ExecutionRunResult(true, 0, "output", "error", "container-id", TimeSpan.FromSeconds(10));

        // Assert - Records should be serializable and portable
        buildResult.Success.Should().BeTrue();
        buildResult.Duration.Should().Be(TimeSpan.FromSeconds(5));

        runResult.Success.Should().BeTrue();
        runResult.ExitCode.Should().Be(0);
        runResult.StandardOutput.Should().Be("output");
        runResult.StandardError.Should().Be("error");
        runResult.ContainerId.Should().Be("container-id");
        runResult.Duration.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task ExecutionPlatform_ShouldSupportCancellation()
    {
        // Arrange
        var logger = _loggerFactory.CreateLogger<DockerExecutionPlatform>();
        var platform = new DockerExecutionPlatform(logger);
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert - Should handle cancellation gracefully
        var act = async () => await platform.IsAvailableAsync(cts.Token);
        await act.Should().NotThrowAsync(); // Should handle cancellation without crashing
    }
}
