using Ashlar.Core.Application.Testing.Models;

namespace Ashlar.Infrastructure.Testing.Docker;

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
