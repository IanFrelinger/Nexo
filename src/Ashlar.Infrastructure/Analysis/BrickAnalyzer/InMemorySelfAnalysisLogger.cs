using System.Collections.Concurrent;
using Ashlar.Core.Application.Analysis.Models;
using Ashlar.Core.Application.Analysis.Ports;

namespace Ashlar.Infrastructure.Analysis.BrickAnalyzer;

/// <summary>
/// In-memory self-analysis logger. Tracks Ashlar's adaptation decisions and outcomes.
/// </summary>
public sealed class InMemorySelfAnalysisLogger : ISelfAnalysisLogger
{
    private readonly ConcurrentQueue<SelfAnalysisEntry> _entries = new();
    private const int MaxEntries = 1000;

    /// <inheritdoc />
    public Task LogAsync(SelfAnalysisEntry entry, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _entries.Enqueue(entry);
        while (_entries.Count > MaxEntries && _entries.TryDequeue(out _))
        {
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SelfAnalysisEntry>> GetRecentAsync(int maxCount = 100, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var arr = _entries.ToArray();
        var skip = Math.Max(0, arr.Length - maxCount);
        var list = arr.Skip(skip).ToList();
        return Task.FromResult<IReadOnlyList<SelfAnalysisEntry>>(list);
    }
}
