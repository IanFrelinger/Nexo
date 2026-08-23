using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Ashlar.Orchestration.Agents.Learning;

/// <summary>
/// Record of feedback for an agent execution.
/// 
/// Contains:
/// - Agent ID and feedback timestamp
/// - Rating (1-5 scale)
/// - Optional comment and execution ID
/// 
/// Used by AgentMemory to track user/system feedback.
/// </summary>
public sealed record FeedbackRecord
{
    /// <summary>Agent that received the feedback.</summary>
    public required string AgentId { get; init; }

    /// <summary>UTC timestamp when feedback was submitted.</summary>
    public required DateTimeOffset ProvidedAt { get; init; }

    /// <summary>Rating on a 1–5 scale.</summary>
    public required int Rating { get; init; } // 1-5 scale

    /// <summary>Optional free-text feedback comment.</summary>
    public string? Comment { get; init; }

    /// <summary>Related execution identifier, if any.</summary>
    public string? ExecutionId { get; init; }
}
