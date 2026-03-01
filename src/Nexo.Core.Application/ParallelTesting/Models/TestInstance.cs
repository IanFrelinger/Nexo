namespace Nexo.Core.Application.ParallelTesting.Models;

/// <summary>
/// A test instance with its parameter set and result.
/// </summary>
public record TestInstance
{
    public required string InstanceId { get; init; }
    public required ParameterSet ParameterSet { get; init; }
    public bool Passed { get; init; }
    public string? Output { get; init; }
    public TimeSpan Duration { get; init; }
}
