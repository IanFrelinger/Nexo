namespace Ashlar.Core.Application.ParallelTesting.Models;

/// <summary>Scenario for parameter matrix generation.</summary>
public record Scenario
{
    /// <summary>Path to the solution or project under test.</summary>
    public required string SolutionOrProjectPath { get; init; }

    /// <summary>xUnit filters to apply across the matrix.</summary>
    public IReadOnlyList<string> Filters { get; init; } = Array.Empty<string>();

    /// <summary>Test category traits to include.</summary>
    public IReadOnlyList<string> Categories { get; init; } = Array.Empty<string>();

    /// <summary>Assembly names to scope test discovery.</summary>
    public IReadOnlyList<string> Assemblies { get; init; } = Array.Empty<string>();
}
