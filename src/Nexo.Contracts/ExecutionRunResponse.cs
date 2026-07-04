namespace Nexo.Contracts;

/// <summary>Result of a container command execution.</summary>
/// <param name="Success">Whether the container run completed without platform errors.</param>
/// <param name="ExitCode">Process exit code inside the container.</param>
/// <param name="StandardOutput">Captured stdout.</param>
/// <param name="StandardError">Captured stderr.</param>
/// <param name="ContainerId">Runtime-assigned container id, when available.</param>
/// <param name="DurationMs">Wall-clock run duration in milliseconds.</param>
public sealed record ExecutionRunResponse(bool Success, int ExitCode, string StandardOutput, string StandardError, string? ContainerId, double DurationMs);
