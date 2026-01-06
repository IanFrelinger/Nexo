using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Nexo.Orchestration.Agents.Learning;

/// <summary>
/// Persistent memory for agents to learn from past executions.
/// 
/// Responsibilities:
/// - Records agent execution history (success/failure, duration, errors)
/// - Records feedback from users or systems
/// - Extracts learned patterns (success rates, common issues, best practices)
/// - Maintains execution history (last 100 executions per agent)
/// 
/// Enables agents to improve over time by learning from past experiences.
/// Thread-safe implementation using concurrent collections.
/// </summary>
public sealed class AgentMemory
{
    private readonly ConcurrentDictionary<string, List<ExecutionRecord>> _executionHistory = new();
    private readonly ConcurrentDictionary<string, List<FeedbackRecord>> _feedbackHistory = new();
    private readonly ILogger<AgentMemory>? _logger;

    public AgentMemory(ILogger<AgentMemory>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Records an agent execution for learning.
    /// </summary>
    public void RecordExecution(string agentId, ExecutionRecord record)
    {
        _executionHistory.AddOrUpdate(
            agentId,
            new List<ExecutionRecord> { record },
            (key, existing) =>
            {
                existing.Add(record);
                // Keep only last 100 executions per agent
                if (existing.Count > 100)
                {
                    existing.RemoveAt(0);
                }
                return existing;
            });

        _logger?.LogDebug("Recorded execution for agent {AgentId}", agentId);
    }

    /// <summary>
    /// Records feedback for an agent execution.
    /// </summary>
    public void RecordFeedback(string agentId, FeedbackRecord feedback)
    {
        _feedbackHistory.AddOrUpdate(
            agentId,
            new List<FeedbackRecord> { feedback },
            (key, existing) =>
            {
                existing.Add(feedback);
                return existing;
            });

        _logger?.LogDebug("Recorded feedback for agent {AgentId}", agentId);
    }

    /// <summary>
    /// Gets execution history for an agent.
    /// </summary>
    public IReadOnlyList<ExecutionRecord> GetExecutionHistory(string agentId, int? limit = null)
    {
        if (!_executionHistory.TryGetValue(agentId, out var history))
        {
            return Array.Empty<ExecutionRecord>();
        }

        if (limit.HasValue)
        {
            return history.TakeLast(limit.Value).ToList();
        }

        return history;
    }

    /// <summary>
    /// Gets feedback history for an agent.
    /// </summary>
    public IReadOnlyList<FeedbackRecord> GetFeedbackHistory(string agentId)
    {
        return _feedbackHistory.TryGetValue(agentId, out var feedback)
            ? feedback
            : Array.Empty<FeedbackRecord>();
    }

    /// <summary>
    /// Gets learned patterns from execution history.
    /// </summary>
    public LearnedPatterns GetLearnedPatterns(string agentId)
    {
        var history = GetExecutionHistory(agentId);
        var feedback = GetFeedbackHistory(agentId);

        return new LearnedPatterns
        {
            AgentId = agentId,
            TotalExecutions = history.Count,
            SuccessRate = history.Count > 0
                ? (double)history.Count(r => r.Success) / history.Count
                : 0.0,
            AverageExecutionTime = history.Count > 0
                ? TimeSpan.FromMilliseconds(history.Average(r => r.Duration.TotalMilliseconds))
                : TimeSpan.Zero,
            CommonIssues = ExtractCommonIssues(history, feedback),
            BestPractices = ExtractBestPractices(history, feedback)
        };
    }

    private List<string> ExtractCommonIssues(
        IReadOnlyList<ExecutionRecord> history,
        IReadOnlyList<FeedbackRecord> feedback)
    {
        var issues = new List<string>();

        // Extract from failed executions
        var failedExecutions = history.Where(r => !r.Success).ToList();
        foreach (var failed in failedExecutions)
        {
            if (!string.IsNullOrWhiteSpace(failed.ErrorMessage))
            {
                issues.Add(failed.ErrorMessage);
            }
        }

        // Extract from negative feedback
        var negativeFeedback = feedback.Where(f => f.Rating < 3).ToList();
        foreach (var fb in negativeFeedback)
        {
            if (!string.IsNullOrWhiteSpace(fb.Comment))
            {
                issues.Add(fb.Comment);
            }
        }

        return issues.Distinct().Take(10).ToList();
    }

    private List<string> ExtractBestPractices(
        IReadOnlyList<ExecutionRecord> history,
        IReadOnlyList<FeedbackRecord> feedback)
    {
        var practices = new List<string>();

        // Extract from successful executions
        var successfulExecutions = history.Where(r => r.Success).ToList();
        if (successfulExecutions.Count > 0)
        {
            practices.Add($"Average successful execution time: {successfulExecutions.Average(r => r.Duration.TotalMilliseconds):F2}ms");
        }

        // Extract from positive feedback
        var positiveFeedback = feedback.Where(f => f.Rating >= 4).ToList();
        foreach (var fb in positiveFeedback)
        {
            if (!string.IsNullOrWhiteSpace(fb.Comment))
            {
                practices.Add(fb.Comment);
            }
        }

        return practices.Distinct().Take(10).ToList();
    }
}

/// <summary>
/// Record of an agent execution.
/// 
/// Contains:
/// - Agent ID and execution timestamp
/// - Duration and success status
/// - Optional error message
/// - Optional metadata dictionary
/// 
/// Used by AgentMemory to track execution history.
/// </summary>
public sealed record ExecutionRecord
{
    public required string AgentId { get; init; }
    public required DateTimeOffset ExecutedAt { get; init; }
    public required TimeSpan Duration { get; init; }
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}

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
    public required string AgentId { get; init; }
    public required DateTimeOffset ProvidedAt { get; init; }
    public required int Rating { get; init; } // 1-5 scale
    public string? Comment { get; init; }
    public string? ExecutionId { get; init; }
}

/// <summary>
/// Patterns learned from agent execution history.
/// 
/// Contains:
/// - Agent ID and execution statistics
/// - Success rate and average execution time
/// - Lists of common issues and best practices
/// 
/// Used by AgentMemory to provide insights for agent improvement.
/// </summary>
public sealed record LearnedPatterns
{
    public required string AgentId { get; init; }
    public required int TotalExecutions { get; init; }
    public required double SuccessRate { get; init; }
    public required TimeSpan AverageExecutionTime { get; init; }
    public IReadOnlyList<string> CommonIssues { get; init; } = new List<string>();
    public IReadOnlyList<string> BestPractices { get; init; } = new List<string>();
}

