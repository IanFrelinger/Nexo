namespace Ashlar.Contracts;

/// <summary>Result of a container image build.</summary>
/// <param name="Success">Whether the build completed successfully.</param>
/// <param name="ErrorMessage">Build error details when <paramref name="Success"/> is false.</param>
/// <param name="DurationMs">Wall-clock build duration in milliseconds.</param>
public sealed record ExecutionBuildResponse(bool Success, string? ErrorMessage, double DurationMs);
