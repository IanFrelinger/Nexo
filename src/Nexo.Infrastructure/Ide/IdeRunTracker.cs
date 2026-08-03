using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Nexo.Infrastructure.Ide;

/// <summary>
/// Process-local tracker for IDE chat/plan/edit/director runs (timeline + cancel).
/// </summary>
public sealed class IdeRunTracker
{
    /// <summary>Shared instance for the API process.</summary>
    public static IdeRunTracker Instance { get; } = new();

    private readonly ConcurrentDictionary<string, Entry> _runs = new(StringComparer.OrdinalIgnoreCase);
    private const int MaxRetained = 100;

    /// <summary>Starts a run and returns its id plus a linked cancellation token.</summary>
    public (string RunId, CancellationToken Token) Start(
        string kind,
        string prompt,
        string? agentId,
        string? modelId,
        CancellationToken requestAborted)
    {
        var runId = Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(requestAborted);
        var record = new IdeRunRecord(
            RunId: runId,
            Kind: kind,
            Status: "running",
            PromptPreview: Truncate(prompt, 160),
            AgentId: agentId,
            ModelId: modelId,
            StartedAtUtc: DateTimeOffset.UtcNow,
            CompletedAtUtc: null,
            Message: null,
            ProposalId: null,
            DailyId: null,
            Error: null);

        _runs[runId] = new Entry(record, cts);
        Trim();
        return (runId, cts.Token);
    }

    /// <summary>Marks a run completed successfully.</summary>
    public void Complete(
        string runId,
        string? message = null,
        string? proposalId = null,
        string? dailyId = null)
    {
        if (!_runs.TryGetValue(runId, out var entry))
            return;
        entry.Record = entry.Record with
        {
            Status = "completed",
            CompletedAtUtc = DateTimeOffset.UtcNow,
            Message = Truncate(message, 400),
            ProposalId = proposalId,
            DailyId = dailyId,
        };
        entry.Cts.Dispose();
    }

    /// <summary>Marks a run failed.</summary>
    public void Fail(string runId, string? error)
    {
        if (!_runs.TryGetValue(runId, out var entry))
            return;
        entry.Record = entry.Record with
        {
            Status = "failed",
            CompletedAtUtc = DateTimeOffset.UtcNow,
            Error = Truncate(error, 400),
        };
        entry.Cts.Dispose();
    }

    /// <summary>Requests cancellation for a running operation.</summary>
    public bool TryCancel(string runId)
    {
        if (!_runs.TryGetValue(runId, out var entry))
            return false;
        if (!string.Equals(entry.Record.Status, "running", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            entry.Cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
            return false;
        }

        entry.Record = entry.Record with
        {
            Status = "cancelled",
            CompletedAtUtc = DateTimeOffset.UtcNow,
            Message = "Cancelled by client",
        };
        try { entry.Cts.Dispose(); } catch { /* ignore */ }
        return true;
    }

    /// <summary>Gets one run by id.</summary>
    public IdeRunRecord? Get(string runId) =>
        _runs.TryGetValue(runId, out var entry) ? entry.Record : null;

    /// <summary>Lists recent runs (newest first).</summary>
    public IReadOnlyList<IdeRunRecord> List(int take = 30) =>
        _runs.Values
            .Select(e => e.Record)
            .OrderByDescending(r => r.StartedAtUtc)
            .Take(Math.Clamp(take, 1, MaxRetained))
            .ToList();

    private void Trim()
    {
        if (_runs.Count <= MaxRetained)
            return;
        var stale = _runs.Values
            .Where(e => e.Record.Status != "running")
            .OrderBy(e => e.Record.StartedAtUtc)
            .Take(_runs.Count - MaxRetained)
            .Select(e => e.Record.RunId)
            .ToList();
        foreach (var id in stale)
            _runs.TryRemove(id, out _);
    }

    private static string Truncate(string? text, int max)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        var t = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return t.Length <= max ? t : t[..max] + "…";
    }

    private sealed class Entry(IdeRunRecord record, CancellationTokenSource cts)
    {
        public IdeRunRecord Record { get; set; } = record;
        public CancellationTokenSource Cts { get; } = cts;
    }
}

/// <summary>One IDE run timeline entry.</summary>
public sealed record IdeRunRecord(
    string RunId,
    string Kind,
    string Status,
    string PromptPreview,
    string? AgentId,
    string? ModelId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? Message,
    string? ProposalId,
    string? DailyId,
    string? Error);
