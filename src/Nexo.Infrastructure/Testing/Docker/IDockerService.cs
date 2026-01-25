using Nexo.Core.Application.Testing.Models;

namespace Nexo.Infrastructure.Testing.Docker;

/// <summary>
/// Service for Docker operations without command-line dependencies.
/// 
/// Provides a portable interface for:
/// - Building Docker images
/// - Running containers
/// - Managing volumes and bind mounts
/// - Executing commands in containers
/// 
/// Uses Docker.DotNet library for cross-platform Docker API access.
/// </summary>
public interface IDockerService
{
    /// <summary>
    /// Checks if Docker is available and accessible.
    /// </summary>
    Task<bool> IsDockerAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds a Docker image from a Dockerfile.
    /// </summary>
    Task<DockerBuildResult> BuildImageAsync(
        string dockerfilePath,
        string imageTag,
        string contextPath,
        Dictionary<string, string>? buildArgs = null,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a container with the specified configuration.
    /// </summary>
    Task<DockerRunResult> RunContainerAsync(
        string imageTag,
        string[] command,
        Dictionary<string, string>? environmentVariables = null,
        Dictionary<string, string>? volumeMounts = null,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// Removes a container by ID.
    /// </summary>
    Task RemoveContainerAsync(string containerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an image by tag.
    /// </summary>
    Task RemoveImageAsync(string imageTag, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a Docker build operation.
/// </summary>
public record DockerBuildResult(bool Success, string? ErrorMessage, TimeSpan Duration);

/// <summary>
/// Result of a Docker run operation.
/// </summary>
public record DockerRunResult(
    bool Success,
    int ExitCode,
    string StandardOutput,
    string StandardError,
    string? ContainerId,
    TimeSpan Duration);

