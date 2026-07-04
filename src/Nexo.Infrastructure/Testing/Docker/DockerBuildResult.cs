using Nexo.Core.Application.Testing.Models;

namespace Nexo.Infrastructure.Testing.Docker;

/// <summary>
/// Result of a Docker build operation.
/// </summary>
public record DockerBuildResult(bool Success, string? ErrorMessage, TimeSpan Duration);
