namespace Nexo.Core.Application.Adaptation.Models;

/// <summary>
/// Context from a failure: what was observed, what was attempted, what broke.
/// Used by fix generator to produce candidate fixes.
/// </summary>
public record FailureContext
{
    public required string FailureType { get; init; }
    public required string TargetId { get; init; }
    public string? Message { get; init; }
    public string? StackTrace { get; init; }
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
