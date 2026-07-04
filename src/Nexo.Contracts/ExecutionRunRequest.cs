namespace Nexo.Contracts;

/// <summary>Request to run a command inside a built container image.</summary>
/// <param name="ImageTag">Image tag produced by a prior build.</param>
/// <param name="Command">Command and arguments to execute.</param>
/// <param name="EnvironmentVariables">Optional container environment variables.</param>
/// <param name="VolumeMounts">Optional host-to-container volume mounts.</param>
/// <param name="WorkingDirectory">Optional working directory inside the container.</param>
public sealed record ExecutionRunRequest(string ImageTag, string[] Command, Dictionary<string, string>? EnvironmentVariables = null, Dictionary<string, string>? VolumeMounts = null, string? WorkingDirectory = null);
