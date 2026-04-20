using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Common.Ports;
using Nexo.Abstractions.Agents;

namespace Nexo.Orchestration.Metrics;

/// <summary>
/// Enhanced metrics collector for orchestration-specific metrics.
/// 
/// Responsibilities:
/// - Records operation execution times and counts
/// - Tracks agent execution metrics (duration, state, resource usage)
/// - Manages distributed tracing spans
/// - Generates performance reports
/// - Integrates with base IMetricsCollector for standard metrics
/// 
/// Thread-safe implementation using concurrent collections.
/// Used throughout orchestration for comprehensive metrics collection.
/// </summary>
public sealed class OrchestrationMetrics
{
    private readonly IMetricsCollector? _baseCollector;
    private readonly ILogger<OrchestrationMetrics> _logger;
    private readonly ConcurrentDictionary<string, OperationMetrics> _operations = new();
    private readonly ConcurrentDictionary<string, AgentMetrics> _agentMetrics = new();
    private readonly ConcurrentDictionary<string, TraceSpan> _spans = new();

    public OrchestrationMetrics(
        ILogger<OrchestrationMetrics> logger,
        IMetricsCollector? baseCollector = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _baseCollector = baseCollector;
    }

    /// <summary>
    /// Records execution time for an orchestration operation.
    /// </summary>
    public void RecordOperation(string operationName, TimeSpan duration, Dictionary<string, object>? metadata = null)
    {
        var metrics = _operations.AddOrUpdate(
            operationName,
            new OperationMetrics
            {
                OperationName = operationName,
                TotalDuration = duration,
                Count = 1,
                MinDuration = duration,
                MaxDuration = duration,
                Metadata = metadata ?? new Dictionary<string, object>()
            },
            (key, existing) =>
            {
                existing.TotalDuration += duration;
                existing.Count++;
                existing.MinDuration = TimeSpan.FromMilliseconds(Math.Min(existing.MinDuration.TotalMilliseconds, duration.TotalMilliseconds));
                existing.MaxDuration = TimeSpan.FromMilliseconds(Math.Max(existing.MaxDuration.TotalMilliseconds, duration.TotalMilliseconds));
                if (metadata != null)
                {
                    foreach (var kvp in metadata)
                    {
                        existing.Metadata[kvp.Key] = kvp.Value;
                    }
                }
                return existing;
            });

        _baseCollector?.RecordExecutionTime($"Orchestration.{operationName}", duration);
        _baseCollector?.IncrementCounter($"Orchestration.{operationName}.Count");

        _logger.LogDebug("Recorded operation {Operation}: {Duration}ms", operationName, duration.TotalMilliseconds);
    }

    /// <summary>
    /// Records metrics for an agent execution.
    /// </summary>
    public void RecordAgentExecution(
        string agentId,
        string domain,
        TimeSpan duration,
        AgentState finalState,
        int? resourceUsageMB = null,
        int? contextTokensUsed = null)
    {
        var metrics = _agentMetrics.AddOrUpdate(
            agentId,
            new AgentMetrics
            {
                AgentId = agentId,
                Domain = domain,
                ExecutionCount = 1,
                TotalDuration = duration,
                MinDuration = duration,
                MaxDuration = duration,
                LastState = finalState,
                TotalResourceUsageMB = resourceUsageMB ?? 0,
                TotalContextTokensUsed = contextTokensUsed ?? 0
            },
            (key, existing) =>
            {
                existing.ExecutionCount++;
                existing.TotalDuration += duration;
                existing.MinDuration = TimeSpan.FromMilliseconds(Math.Min(existing.MinDuration.TotalMilliseconds, duration.TotalMilliseconds));
                existing.MaxDuration = TimeSpan.FromMilliseconds(Math.Max(existing.MaxDuration.TotalMilliseconds, duration.TotalMilliseconds));
                existing.LastState = finalState;
                existing.TotalResourceUsageMB += resourceUsageMB ?? 0;
                existing.TotalContextTokensUsed += contextTokensUsed ?? 0;
                return existing;
            });

        _baseCollector?.RecordExecutionTime($"Agent.{domain}.{agentId}", duration);
        _baseCollector?.IncrementCounter($"Agent.{domain}.Executed");
        _baseCollector?.IncrementCounter($"Agent.State.{finalState}");

        if (resourceUsageMB.HasValue)
        {
            _baseCollector?.IncrementCounter($"Agent.Resource.MemoryMB", resourceUsageMB.Value);
        }

        _logger.LogDebug("Recorded agent metrics {AgentId}: {Duration}ms, state={State}", agentId, duration.TotalMilliseconds, finalState);
    }

    /// <summary>
    /// Starts a trace span for distributed tracing.
    /// </summary>
    public string StartSpan(string operationName, string? parentSpanId = null, Dictionary<string, string>? tags = null)
    {
        var spanId = Guid.NewGuid().ToString();
        var span = new TraceSpan
        {
            SpanId = spanId,
            OperationName = operationName,
            ParentSpanId = parentSpanId,
            StartTime = DateTimeOffset.UtcNow,
            Tags = tags ?? new Dictionary<string, string>()
        };

        _spans[spanId] = span;
        _logger.LogDebug("Started span {SpanId}: {Operation}", spanId, operationName);
        return spanId;
    }

    /// <summary>
    /// Ends a trace span.
    /// </summary>
    public void EndSpan(string spanId, bool success = true, Dictionary<string, string>? additionalTags = null)
    {
        if (!_spans.TryGetValue(spanId, out var span))
        {
            _logger.LogWarning("Attempted to end non-existent span {SpanId}", spanId);
            return;
        }

        span.EndTime = DateTimeOffset.UtcNow;
        span.Duration = span.EndTime.Value - span.StartTime;
        span.Success = success;

        if (additionalTags != null)
        {
            foreach (var kvp in additionalTags)
            {
                span.Tags[kvp.Key] = kvp.Value;
            }
        }

        _logger.LogDebug("Ended span {SpanId}: {Duration}ms, success={Success}", spanId, span.Duration.TotalMilliseconds, success);
    }

    /// <summary>
    /// Gets a performance report for all operations.
    /// </summary>
    public PerformanceReport GetPerformanceReport()
    {
        var operations = _operations.Values
            .Select(m => new OperationReport
            {
                OperationName = m.OperationName,
                Count = m.Count,
                TotalDuration = m.TotalDuration,
                AverageDuration = TimeSpan.FromMilliseconds(m.TotalDuration.TotalMilliseconds / m.Count),
                MinDuration = m.MinDuration,
                MaxDuration = m.MaxDuration,
                Metadata = m.Metadata
            })
            .OrderByDescending(o => o.TotalDuration)
            .ToList();

        var agentReports = _agentMetrics.Values
            .Select(m => new AgentReport
            {
                AgentId = m.AgentId,
                Domain = m.Domain,
                ExecutionCount = m.ExecutionCount,
                AverageDuration = TimeSpan.FromMilliseconds(m.TotalDuration.TotalMilliseconds / m.ExecutionCount),
                MinDuration = m.MinDuration,
                MaxDuration = m.MaxDuration,
                LastState = m.LastState,
                AverageResourceUsageMB = m.ExecutionCount > 0 ? m.TotalResourceUsageMB / m.ExecutionCount : 0,
                AverageContextTokensUsed = m.ExecutionCount > 0 ? m.TotalContextTokensUsed / m.ExecutionCount : 0
            })
            .OrderByDescending(a => a.ExecutionCount)
            .ToList();

        var traceSpans = _spans.Values
            .Where(s => s.EndTime.HasValue)
            .Select(s => new SpanReport
            {
                SpanId = s.SpanId,
                OperationName = s.OperationName,
                ParentSpanId = s.ParentSpanId,
                Duration = s.Duration,
                Success = s.Success,
                Tags = s.Tags
            })
            .ToList();

        return new PerformanceReport
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            Operations = operations,
            Agents = agentReports,
            Traces = traceSpans
        };
    }

    /// <summary>
    /// Gets metrics for a specific agent.
    /// </summary>
    public AgentMetrics? GetAgentMetrics(string agentId)
    {
        return _agentMetrics.TryGetValue(agentId, out var metrics) ? metrics : null;
    }

    /// <summary>
    /// Gets all trace spans for a correlation ID or operation.
    /// </summary>
    public IReadOnlyList<TraceSpan> GetTraces(string? correlationId = null, string? operationName = null)
    {
        return _spans.Values
            .Where(s =>
                (correlationId == null || s.Tags.TryGetValue("correlationId", out var cid) && cid == correlationId) &&
                (operationName == null || s.OperationName == operationName))
            .ToList();
    }

    /// <summary>
    /// Clears all collected metrics.
    /// </summary>
    public void Clear()
    {
        _operations.Clear();
        _agentMetrics.Clear();
        _spans.Clear();
        _logger.LogInformation("Cleared all orchestration metrics");
    }
}

/// <summary>
/// Metrics for a specific operation.
/// 
/// Contains:
/// - Operation name
/// - Duration statistics (total, min, max, count)
/// - Optional metadata dictionary
/// 
/// Used internally by OrchestrationMetrics to track operation performance.
/// </summary>
internal sealed class OperationMetrics
{
    public required string OperationName { get; init; }
    public TimeSpan TotalDuration { get; set; }
    public int Count { get; set; }
    public TimeSpan MinDuration { get; set; }
    public TimeSpan MaxDuration { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Metrics for agent execution.
/// 
/// Contains:
/// - Agent ID and domain
/// - Execution statistics (count, duration min/max/total)
/// - Last execution state
/// - Resource usage (memory, context tokens)
/// 
/// Used by OrchestrationMetrics to track agent performance.
/// </summary>
public sealed class AgentMetrics
{
    public required string AgentId { get; init; }
    public required string Domain { get; init; }
    public int ExecutionCount { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan MinDuration { get; set; }
    public TimeSpan MaxDuration { get; set; }
    public AgentState LastState { get; set; }
    public long TotalResourceUsageMB { get; set; }
    public long TotalContextTokensUsed { get; set; }
}

/// <summary>
/// Trace span for distributed tracing.
/// 
/// Contains:
/// - Span identification (ID, parent ID, operation name)
/// - Timing information (start, end, duration)
/// - Success status and optional error message
/// - Tags dictionary for additional metadata
/// 
/// Used by OrchestrationMetrics for distributed tracing.
/// </summary>
public sealed class TraceSpan
{
    public required string SpanId { get; init; }
    public required string OperationName { get; init; }
    public string? ParentSpanId { get; init; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
}

/// <summary>
/// Performance report containing all collected metrics.
/// 
/// Contains:
/// - Generation timestamp
/// - Lists of operation reports, agent reports, and trace spans
/// 
/// Produced by OrchestrationMetrics.GetPerformanceReport.
/// Used for performance analysis and reporting.
/// </summary>
public sealed record PerformanceReport
{
    public required DateTimeOffset GeneratedAt { get; init; }
    public required IReadOnlyList<OperationReport> Operations { get; init; }
    public required IReadOnlyList<AgentReport> Agents { get; init; }
    public required IReadOnlyList<SpanReport> Traces { get; init; }
}

/// <summary>
/// Report for a single operation.
/// 
/// Contains:
/// - Operation name and execution count
/// - Duration statistics (total, average, min, max)
/// - Optional metadata dictionary
/// 
/// Used by PerformanceReport to report operation metrics.
/// </summary>
public sealed record OperationReport
{
    public required string OperationName { get; init; }
    public required int Count { get; init; }
    public required TimeSpan TotalDuration { get; init; }
    public required TimeSpan AverageDuration { get; init; }
    public required TimeSpan MinDuration { get; init; }
    public required TimeSpan MaxDuration { get; init; }
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// Report for agent execution.
/// 
/// Contains:
/// - Agent ID and domain
/// - Execution statistics (count, duration min/max/average)
/// - Resource usage averages (memory, context tokens)
/// 
/// Used by PerformanceReport to report agent metrics.
/// </summary>
public sealed record AgentReport
{
    public required string AgentId { get; init; }
    public required string Domain { get; init; }
    public required int ExecutionCount { get; init; }
    public required TimeSpan AverageDuration { get; init; }
    public required TimeSpan MinDuration { get; init; }
    public required TimeSpan MaxDuration { get; init; }
    public required AgentState LastState { get; init; }
    public required long AverageResourceUsageMB { get; init; }
    public required long AverageContextTokensUsed { get; init; }
}

/// <summary>
/// Report for a trace span.
/// </summary>
public sealed record SpanReport
{
    public required string SpanId { get; init; }
    public required string OperationName { get; init; }
    public string? ParentSpanId { get; init; }
    public required TimeSpan Duration { get; init; }
    public required bool Success { get; init; }
    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();
}

