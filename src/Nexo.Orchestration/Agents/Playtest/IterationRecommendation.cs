using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Playtest.Models;
using System.Text.Json;

namespace Nexo.Orchestration.Agents.Playtest;

/// <summary>
/// Recommendation for iteration.
/// 
/// Contains:
/// - Whether iteration is recommended
/// - Priority level and estimated effort
/// - Optional summary text
/// 
/// Used by FeedbackSynthesizerAgent to recommend next steps.
/// </summary>
public sealed record IterationRecommendation
{
    /// <summary>Whether another design iteration is recommended.</summary>
    public bool ShouldIterate { get; init; }

    /// <summary>Priority assigned to the recommended iteration.</summary>
    public IterationPriority Priority { get; init; }

    /// <summary>Estimated effort for the iteration.</summary>
    public TimeSpan EstimatedEffort { get; init; }

    /// <summary>Optional summary of the recommendation.</summary>
    public string? Summary { get; init; }
}
