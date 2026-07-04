using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Negotiation.Models;

/// <summary>
/// Result of impact analysis for a single agent.
/// 
/// Contains:
/// - Agent ID
/// - Impact score if this agent yields (0-1, lower = less impact = should yield first)
/// - Description of what happens if this agent yields
/// 
/// Used by negotiation strategies to determine which agent should yield.
/// </summary>
public sealed record ImpactAnalysis
{
    public required string AgentId { get; init; }

    /// <summary>
    /// Impact score if this agent yields (0-1). Lower = less impact = should yield first.
    /// </summary>
    public double ImpactScore { get; init; }

    /// <summary>
    /// Description of what happens if this agent yields.
    /// </summary>
    public string? ImpactDescription { get; init; }
}
