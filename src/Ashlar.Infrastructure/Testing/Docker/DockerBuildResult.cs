using Ashlar.Core.Application.Testing.Models;

namespace Ashlar.Infrastructure.Testing.Docker;

/// <summary>
/// Result of a Docker build operation.
/// </summary>
public record DockerBuildResult(bool Success, string? ErrorMessage, TimeSpan Duration);
