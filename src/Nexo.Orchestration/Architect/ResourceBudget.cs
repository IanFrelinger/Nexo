namespace Nexo.Orchestration.Architect;

using Models;

/// <summary>
/// Resource budget available for agent execution.
/// </summary>
public sealed record ResourceBudget
{
    /// <summary>
    /// Maximum total compute time in seconds.
    /// </summary>
    public int? MaxComputeSeconds { get; init; }

    /// <summary>
    /// Maximum total context tokens.
    /// </summary>
    public int? MaxContextTokens { get; init; }

    /// <summary>
    /// Maximum total memory in MB.
    /// </summary>
    public int? MaxMemoryMB { get; init; }
}
