namespace Nexo.Infrastructure.Testing.ExecutionPlatform;

/// <summary>
/// Result of a container run operation.
/// </summary>
public record ExecutionRunResult(
    bool Success,
    int ExitCode,
    string StandardOutput,
    string StandardError,
    string? ContainerId,
    TimeSpan Duration);
