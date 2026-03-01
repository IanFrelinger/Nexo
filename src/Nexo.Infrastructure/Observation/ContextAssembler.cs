using System.Text;
using Nexo.Core.Application.Observation.Models;
using Nexo.Core.Application.Observation.Ports;

namespace Nexo.Infrastructure.Observation;

/// <summary>
/// Assembles observation context for agent consumption.
/// </summary>
public sealed class ContextAssembler : IContextAssembler
{
    private readonly IPatternStore _store;

    /// <summary>
    /// Creates a context assembler.
    /// </summary>
    public ContextAssembler(IPatternStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <inheritdoc />
    public async Task<ObservationContext> AssembleContextAsync(
        string? projectPath = null,
        DateTimeOffset? since = null,
        DateTimeOffset? until = null,
        int maxPatterns = 50,
        bool includeRecentEvents = false,
        CancellationToken cancellationToken = default)
    {
        var query = new PatternStoreQueryParams
        {
            ProjectPath = projectPath,
            Since = since,
            Until = until,
            MaxCount = maxPatterns,
        };

        var patterns = await _store.QueryAsync(query, cancellationToken).ConfigureAwait(false);
        var summary = BuildSummary(patterns);
        return new ObservationContext
        {
            Patterns = patterns,
            RecentEvents = includeRecentEvents ? Array.Empty<NormalizedEvent>() : Array.Empty<NormalizedEvent>(),
            Summary = summary,
        };
    }

    private static string BuildSummary(IReadOnlyList<ObservedPattern> patterns)
    {
        if (patterns.Count == 0)
            return "No observed patterns in the given range.";

        var sb = new StringBuilder();
        sb.AppendLine($"Observed {patterns.Count} pattern(s):");
        sb.AppendLine();

        foreach (var p in patterns.Take(20))
        {
            sb.AppendLine($"- **{p.EventType}** (frequency: {p.Frequency}, last seen: {p.LastSeen:yyyy-MM-dd HH:mm:ss})");
            if (!string.IsNullOrEmpty(p.ProjectPath))
                sb.AppendLine($"  Project: {p.ProjectPath}");
        }

        if (patterns.Count > 20)
            sb.AppendLine($"... and {patterns.Count - 20} more");

        return sb.ToString();
    }
}
