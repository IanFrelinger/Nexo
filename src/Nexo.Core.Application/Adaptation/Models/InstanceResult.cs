namespace Nexo.Core.Application.Adaptation.Models;

/// <summary>
/// Result from a single test instance.
/// </summary>
public record InstanceResult
{
    public required string InstanceId { get; init; }
    public required IReadOnlyDictionary<string, object> ParameterSet { get; init; }
    public required bool Passed { get; init; }
    public IReadOnlyList<AdaptationRecord>? Adaptations { get; init; }
    public TimeSpan Duration { get; init; }
}
