using System.Text.Json;
using Nexo.Abstractions.Execution;

namespace Nexo.Orchestration.Architect.Models;

/// <summary>
/// Resource requirements for an agent.
/// 
/// Contains:
/// - Estimated compute time in seconds
/// - Required context window size in tokens
/// - Memory requirements in MB
/// 
/// Used by ResourceAllocator to allocate resources to agents.
/// </summary>
public sealed record ResourceRequirements
{
    /// <summary>
    /// Estimated compute time in seconds.
    /// </summary>
    public int? EstimatedComputeSeconds { get; init; }

    /// <summary>
    /// Required context window size in tokens.
    /// </summary>
    public int? RequiredContextTokens { get; init; }

    /// <summary>
    /// Memory requirements in MB.
    /// </summary>
    public int? RequiredMemoryMB { get; init; }
}
