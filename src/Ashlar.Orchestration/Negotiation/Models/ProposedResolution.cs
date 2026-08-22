using Ashlar.Orchestration.Architect.Models;

namespace Ashlar.Orchestration.Negotiation.Models;

/// <summary>
/// A proposed resolution to a conflict.
/// 
/// Contains:
/// - Proposer ID and description
/// - Required changes from each agent (agentId → required changes)
/// - Resolved artifact (schema, resource allocation, etc.)
/// - Confidence that this resolution will work (0-1)
/// 
/// Used by NegotiationProtocol to propose conflict resolutions.
/// </summary>
public sealed record ProposedResolution
{
    public required string ProposerId { get; init; }
    public required string Description { get; init; }

    /// <summary>
    /// Changes required from each agent (agentId → required changes).
    /// </summary>
    public IReadOnlyDictionary<string, string> RequiredChanges { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// The resolved artifact (schema, resource allocation, etc.)
    /// </summary>
    public object? ResolvedArtifact { get; init; }

    /// <summary>
    /// Confidence that this resolution will work (0-1).
    /// </summary>
    public double Confidence { get; init; }
}
