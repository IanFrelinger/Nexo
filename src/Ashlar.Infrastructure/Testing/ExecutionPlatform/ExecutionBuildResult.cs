namespace Ashlar.Infrastructure.Testing.ExecutionPlatform;

/// <summary>
/// Result of a container image build operation.
/// </summary>
public record ExecutionBuildResult(
    bool Success,
    string? ErrorMessage,
    TimeSpan Duration);
