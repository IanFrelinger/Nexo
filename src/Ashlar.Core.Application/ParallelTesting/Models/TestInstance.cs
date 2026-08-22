namespace Ashlar.Core.Application.ParallelTesting.Models;

/// <summary>A test instance with its parameter set and result.</summary>
public record TestInstance
{
    /// <summary>Unique identifier for this matrix instance.</summary>
    public required string InstanceId { get; init; }

    /// <summary>Parameter values exercised in this instance.</summary>
    public required ParameterSet ParameterSet { get; init; }

    /// <summary>Whether this instance passed.</summary>
    public bool Passed { get; init; }

    /// <summary>Captured test output, when available.</summary>
    public string? Output { get; init; }

    /// <summary>Wall-clock duration of this instance run.</summary>
    public TimeSpan Duration { get; init; }
}
