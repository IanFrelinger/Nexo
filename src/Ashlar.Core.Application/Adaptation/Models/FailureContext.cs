namespace Ashlar.Core.Application.Adaptation.Models;

/// <summary>
/// Context from a failure: what was observed, what was attempted, what broke.
/// Used by fix generator to produce candidate fixes.
/// </summary>
public record FailureContext
{
    /// <summary>Failure type label (e.g. regression, compile, runtime).</summary>
    public required string FailureType { get; init; }

    /// <summary>Brick, behavior, or file identifier that failed.</summary>
    public required string TargetId { get; init; }

    /// <summary>Human-readable failure message.</summary>
    public string? Message { get; init; }

    /// <summary>Stack trace when a runtime exception occurred.</summary>
    public string? StackTrace { get; init; }

    /// <summary>Additional failure metadata for fix generation.</summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
