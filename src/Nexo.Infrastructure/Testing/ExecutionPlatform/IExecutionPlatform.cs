namespace Nexo.Infrastructure.Testing.ExecutionPlatform;

/// <summary>
/// Abstraction for execution platforms (Docker, Rancher, Kubernetes, etc.).
/// 
/// Allows users to plug in different container/orchestration platforms
/// for multi-platform testing without being tied to a specific implementation.
/// </summary>
public interface IExecutionPlatform
{
    /// <summary>
    /// Gets the name of the execution platform (e.g., "Docker", "Rancher", "Kubernetes").
    /// </summary>
    string PlatformName { get; }

    /// <summary>
    /// Checks if the execution platform is available and ready to use.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds a container image from a Dockerfile.
    /// </summary>
    /// <param name="dockerfilePath">Path to the Dockerfile</param>
    /// <param name="imageTag">Tag for the built image</param>
    /// <param name="contextPath">Build context path</param>
    /// <param name="buildArgs">Build arguments</param>
    /// <param name="progress">Optional progress callback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Build result with success status and error information</returns>
    Task<ExecutionBuildResult> BuildImageAsync(
        string dockerfilePath,
        string imageTag,
        string contextPath,
        Dictionary<string, string>? buildArgs = null,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a container with the specified command.
    /// </summary>
    /// <param name="imageTag">Image tag to run</param>
    /// <param name="command">Command to execute in the container</param>
    /// <param name="environmentVariables">Environment variables</param>
    /// <param name="volumeMounts">Volume mounts (host path -> container path)</param>
    /// <param name="workingDirectory">Working directory in container</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Run result with exit code, output, and error information</returns>
    Task<ExecutionRunResult> RunContainerAsync(
        string imageTag,
        string[] command,
        Dictionary<string, string>? environmentVariables = null,
        Dictionary<string, string>? volumeMounts = null,
        string? workingDirectory = null,
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
