using System.Text;
using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Core.Application.Observation.Models;
using Ashlar.Core.Application.Observation.Ports;
using Ashlar.Core.Application.SelfContext.Models;
using Ashlar.Core.Application.SelfContext.Ports;

namespace Ashlar.Infrastructure.SelfContext;

/// <summary>
/// Assembles self-context from adaptation log, execution tracer, and pattern store.
/// </summary>
public sealed class SelfContextAssembler : ISelfContextAssembler
{
    private readonly IAdaptationLog _adaptationLog;
    private readonly IExecutionTracer _executionTracer;
    private readonly IPatternStore _patternStore;

    /// <summary>Initializes a new self context assembler.</summary>
    public SelfContextAssembler(IAdaptationLog adaptationLog, IExecutionTracer executionTracer, IPatternStore patternStore)
    {
        _adaptationLog = adaptationLog ?? throw new ArgumentNullException(nameof(adaptationLog));
        _executionTracer = executionTracer ?? throw new ArgumentNullException(nameof(executionTracer));
        _patternStore = patternStore ?? throw new ArgumentNullException(nameof(patternStore));
    }

    /// <inheritdoc />
    public async Task<SelfContextModel> AssembleAsync(TimeSpan? lookback = null, CancellationToken cancellationToken = default)
    {
        var since = lookback.HasValue ? DateTimeOffset.UtcNow - lookback.Value : (DateTimeOffset?)null;
        var until = DateTimeOffset.UtcNow;

        var adaptationsTask = _adaptationLog.QueryAsync(since, until, null, cancellationToken);
        var executionsTask = _executionTracer.QueryAsync(since, until, cancellationToken);
        var patternsTask = _patternStore.QueryAsync(new PatternStoreQueryParams { Since = since, Until = until, MaxCount = 50 }, cancellationToken);

        await Task.WhenAll(adaptationsTask, executionsTask, patternsTask).ConfigureAwait(false);

        var adaptations = await adaptationsTask.ConfigureAwait(false);
        var executions = await executionsTask.ConfigureAwait(false);
        var patterns = await patternsTask.ConfigureAwait(false);

        var summary = BuildSummary(adaptations, executions, patterns);
        return new SelfContextModel
        {
            RecentAdaptations = adaptations,
            RecentExecutions = executions,
            RecentPatterns = patterns,
            Summary = summary,
        };
    }

    private static string BuildSummary(
        IReadOnlyList<AdaptationRecord> adaptations,
        IReadOnlyList<ExecutionTraceEntry> executions,
        IReadOnlyList<ObservedPattern> patterns)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Recent Activity Summary");
        sb.AppendLine();
        sb.AppendLine($"### Adaptations ({adaptations.Count})");
        foreach (var a in adaptations.Take(10))
        {
            var status = a.Promoted ? "promoted" : (a.RegressionPassed ? "rejected" : "rollback");
            sb.AppendLine($"- {a.Timestamp:yyyy-MM-dd HH:mm} | {a.FailureType} | {a.FilePath ?? "?"} | {status}");
        }
        sb.AppendLine();
        sb.AppendLine($"### Executions ({executions.Count})");
        foreach (var e in executions.Take(10))
            sb.AppendLine($"- {e.Timestamp:yyyy-MM-dd HH:mm} | {e.Operation} | {e.Path ?? "-"} | {e.Outcome ?? "-"}");
        sb.AppendLine();
        sb.AppendLine($"### Patterns ({patterns.Count})");
        foreach (var p in patterns.Take(10))
            sb.AppendLine($"- {p.EventType} | freq={p.Frequency} | last={p.LastSeen:yyyy-MM-dd HH:mm}");
        return sb.ToString();
    }
}
