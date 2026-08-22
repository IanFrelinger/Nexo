namespace Ashlar.Contracts;

/// <summary>Request to build a container image for remote test execution.</summary>
/// <param name="DockerfilePath">Path to the Dockerfile within the build context.</param>
/// <param name="ImageTag">Tag to assign to the built image.</param>
/// <param name="ContextPath">Docker build context directory.</param>
/// <param name="BuildArgs">Optional build-time arguments.</param>
public sealed record ExecutionBuildRequest(string DockerfilePath, string ImageTag, string ContextPath, Dictionary<string, string>? BuildArgs = null);
