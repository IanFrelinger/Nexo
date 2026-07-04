using System.Text.Json;
using Nexo.Orchestration.Negotiation.Models;

namespace Nexo.Orchestration.Negotiation;

/// <summary>
/// Result of a negotiation attempt.
/// 
/// Represents the outcome of conflict negotiation:
/// - Success status and resolution type
/// - Resolved artifacts (schemas, resource allocations)
/// - Proposed resolutions
/// - Reason for result
/// - Number of negotiation rounds required
/// 
/// Used by NegotiationProtocol to communicate negotiation outcomes.
/// </summary>
public sealed record NegotiationResult
{
    /// <summary>Whether negotiation produced a resolution.</summary>
    public bool Success { get; init; }

    /// <summary>How the conflict was resolved.</summary>
    public ResolutionType ResolutionType { get; init; }

    /// <summary>Negotiated resolution proposal, if applicable.</summary>
    public ProposedResolution? Resolution { get; init; }

    /// <summary>Resolved schema artifact, if applicable.</summary>
    public JsonElement? ResolvedSchema { get; init; }

    /// <summary>Resolved resource allocation, if applicable.</summary>
    public ResourceAllocation? ResolvedAllocation { get; init; }

    /// <summary>Human-readable explanation of the outcome.</summary>
    public string? Reason { get; init; }

    /// <summary>Number of negotiation rounds required.</summary>
    public int RoundsRequired { get; init; }

    public static NegotiationResult Negotiated(ProposedResolution resolution, string reason, int rounds = 0) => new()
    {
        Success = true,
        ResolutionType = ResolutionType.Negotiated,
        Resolution = resolution,
        Reason = reason,
        RoundsRequired = rounds
    };

    public static NegotiationResult Synthesized(ProposedResolution resolution, string reason) => new()
    {
        Success = true,
        ResolutionType = ResolutionType.Synthesized,
        Resolution = resolution,
        Reason = reason
    };

    public static NegotiationResult SchemaResolved(JsonElement schema, string reason) => new()
    {
        Success = true,
        ResolutionType = ResolutionType.Automatic,
        ResolvedSchema = schema,
        Reason = reason
    };

    public static NegotiationResult ResourceResolved(ResourceAllocation allocation, string reason) => new()
    {
        Success = true,
        ResolutionType = ResolutionType.Automatic,
        ResolvedAllocation = allocation,
        Reason = reason
    };

    public static NegotiationResult Escalated(string reason) => new()
    {
        Success = false,
        ResolutionType = ResolutionType.Escalated,
        Reason = reason
    };
}
