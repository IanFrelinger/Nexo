using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Playtest.Models;
using System.Text.Json;

namespace Nexo.Orchestration.Agents.Playtest;

/// <summary>
/// Result of feedback synthesis.
/// 
/// Contains:
/// - Synthesis ID and source report ID
/// - Generation timestamp
/// - List of design change requests
/// - Optional iteration recommendation
/// 
/// Produced by FeedbackSynthesizerAgent after processing playtest feedback.
/// </summary>
public sealed record FeedbackSynthesis
{
    /// <summary>Unique synthesis result identifier.</summary>
    public required string SynthesisId { get; init; }

    /// <summary>Playtest report that was synthesized.</summary>
    public required string SourceReportId { get; init; }

    /// <summary>UTC timestamp when synthesis completed.</summary>
    public DateTimeOffset GeneratedAt { get; init; }

    /// <summary>Proposed design changes derived from feedback.</summary>
    public IReadOnlyList<DesignChangeRequest> ChangeRequests { get; init; } = Array.Empty<DesignChangeRequest>();

    /// <summary>Optional recommendation for the next iteration.</summary>
    public IterationRecommendation? IterationRecommendation { get; init; }
}
