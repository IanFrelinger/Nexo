using System.Text.Json;
using Nexo.Orchestration.Negotiation.Models;

namespace Nexo.Orchestration.Negotiation;

/// <summary>
/// Result of a negotiation attempt.
/// </summary>
public sealed record NegotiationResult
{
    public bool Success { get; init; }
    public ResolutionType ResolutionType { get; init; }
    public ProposedResolution? Resolution { get; init; }
    public JsonElement? ResolvedSchema { get; init; }
    public ResourceAllocation? ResolvedAllocation { get; init; }
    public string? Reason { get; init; }
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

/// <summary>
/// Type of resolution achieved.
/// </summary>
public enum ResolutionType
{
    /// <summary>Resolved automatically without agent input.</summary>
    Automatic,

    /// <summary>Resolved through agent negotiation.</summary>
    Negotiated,

    /// <summary>Resolved through creative synthesis.</summary>
    Synthesized,

    /// <summary>Could not be resolved - escalated to human.</summary>
    Escalated
}

