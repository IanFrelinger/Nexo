namespace Nexo.Core.Application.ParallelTesting.Models;

/// <summary>
/// Scenario for parameter matrix generation.
/// </summary>
public record Scenario
{
    public required string SolutionOrProjectPath { get; init; }
    public IReadOnlyList<string> Filters { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Categories { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Assemblies { get; init; } = Array.Empty<string>();
}
