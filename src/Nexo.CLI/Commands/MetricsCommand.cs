using Microsoft.Extensions.Logging;
using Nexo.CLI.Formatting;
using Nexo.Orchestration.Metrics;

namespace Nexo.CLI.Commands;

/// <summary>
/// CLI command for viewing orchestration metrics and performance data.
/// </summary>
public class MetricsCommand
{
    private readonly OrchestrationMetrics _metrics;
    private readonly IConsoleRenderer _renderer;
    private readonly ILogger<MetricsCommand> _logger;

    public MetricsCommand(
        OrchestrationMetrics metrics,
        IConsoleRenderer renderer,
        ILogger<MetricsCommand> logger)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Shows a performance report.
    /// </summary>
    public Task<int> ShowReportAsync(bool json, bool verbose)
    {
        var report = _metrics.GetPerformanceReport();

        if (json)
        {
            var jsonData = new
            {
                ok = true,
                generatedAt = report.GeneratedAt,
                operations = report.Operations.Select(o => new
                {
                    name = o.OperationName,
                    count = o.Count,
                    totalDuration = o.TotalDuration.TotalMilliseconds,
                    averageDuration = o.AverageDuration.TotalMilliseconds,
                    minDuration = o.MinDuration.TotalMilliseconds,
                    maxDuration = o.MaxDuration.TotalMilliseconds,
                    metadata = o.Metadata
                }),
                agents = report.Agents.Select(a => new
                {
                    agentId = a.AgentId,
                    domain = a.Domain,
                    executionCount = a.ExecutionCount,
                    averageDuration = a.AverageDuration.TotalMilliseconds,
                    minDuration = a.MinDuration.TotalMilliseconds,
                    maxDuration = a.MaxDuration.TotalMilliseconds,
                    lastState = a.LastState.ToString(),
                    averageResourceUsageMB = a.AverageResourceUsageMB,
                    averageContextTokensUsed = a.AverageContextTokensUsed
                }),
                traces = report.Traces.Select(t => new
                {
                    spanId = t.SpanId,
                    operationName = t.OperationName,
                    parentSpanId = t.ParentSpanId,
                    duration = t.Duration.TotalMilliseconds,
                    success = t.Success,
                    tags = t.Tags
                })
            };
            _renderer.RenderJson(jsonData);
        }
        else
        {
            _renderer.RenderPerformanceReport(report, verbose);
        }

        return Task.FromResult((int)ExitCode.Ok);
    }

    /// <summary>
    /// Shows metrics for a specific agent.
    /// </summary>
    public Task<int> ShowAgentAsync(string agentId, bool json, bool verbose)
    {
        var metrics = _metrics.GetAgentMetrics(agentId);

        if (metrics == null)
        {
            _renderer.RenderError($"No metrics found for agent '{agentId}'");
            return Task.FromResult((int)ExitCode.ValidationFailed);
        }

        if (json)
        {
            var jsonData = new
            {
                ok = true,
                agent = new
                {
                    agentId = metrics.AgentId,
                    domain = metrics.Domain,
                    executionCount = metrics.ExecutionCount,
                    averageDuration = (metrics.TotalDuration.TotalMilliseconds / metrics.ExecutionCount),
                    minDuration = metrics.MinDuration.TotalMilliseconds,
                    maxDuration = metrics.MaxDuration.TotalMilliseconds,
                    lastState = metrics.LastState.ToString(),
                    totalResourceUsageMB = metrics.TotalResourceUsageMB,
                    totalContextTokensUsed = metrics.TotalContextTokensUsed
                }
            };
            _renderer.RenderJson(jsonData);
        }
        else
        {
            _renderer.RenderAgentMetrics(metrics, verbose);
        }

        return Task.FromResult((int)ExitCode.Ok);
    }

    /// <summary>
    /// Shows trace spans for a correlation ID or operation.
    /// </summary>
    public Task<int> ShowTracesAsync(string? correlationId, string? operationName, bool json, bool verbose)
    {
        var traces = _metrics.GetTraces(correlationId, operationName);

        if (json)
        {
            var jsonData = new
            {
                ok = true,
                count = traces.Count,
                traces = traces.Select(t => new
                {
                    spanId = t.SpanId,
                    operationName = t.OperationName,
                    parentSpanId = t.ParentSpanId,
                    startTime = t.StartTime,
                    endTime = t.EndTime,
                    duration = t.EndTime.HasValue ? (t.EndTime.Value - t.StartTime).TotalMilliseconds : (double?)null,
                    success = t.Success,
                    tags = t.Tags
                })
            };
            _renderer.RenderJson(jsonData);
        }
        else
        {
            _renderer.RenderTraces(traces, correlationId, operationName, verbose);
        }

        return Task.FromResult((int)ExitCode.Ok);
    }

    /// <summary>
    /// Clears all collected metrics.
    /// </summary>
    public Task<int> ClearAsync(bool json, bool verbose)
    {
        _metrics.Clear();

        if (json)
        {
            _renderer.RenderJson(new { ok = true, message = "Metrics cleared" });
        }
        else
        {
            _renderer.RenderSuccess("All metrics cleared");
        }

        return Task.FromResult((int)ExitCode.Ok);
    }
}

