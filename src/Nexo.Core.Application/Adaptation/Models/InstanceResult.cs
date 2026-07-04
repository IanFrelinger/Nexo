namespace Nexo.Core.Application.Adaptation.Models;

/// <summary>
/// Result from a single test instance.
/// </summary>
public record InstanceResult
{
    /// <summary>Identifier of the test instance that produced this result.</summary>
    public required string InstanceId { get; init; }

    /// <summary>Parameter set used for this instance run.</summary>
    public required IReadOnlyDictionary<string, object> ParameterSet { get; init; }

    /// <summary>Whether this instance passed all checks.</summary>
    public required bool Passed { get; init; }

    /// <summary>Adaptation records produced during this instance run.</summary>
    public IReadOnlyList<AdaptationRecord>? Adaptations { get; init; }

    /// <summary>Wall-clock duration of the instance run.</summary>
    public TimeSpan Duration { get; init; }
}
