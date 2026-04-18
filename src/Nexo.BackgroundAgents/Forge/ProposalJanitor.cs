namespace Nexo.BackgroundAgents.Forge;

/// <summary>
/// Auto-stales change proposals that have been sitting in <c>Proposed</c> or
/// <c>Approved</c> longer than the configured TTL. Without this, the queue
/// silently grows forever as the operator forgets about old asks and the agent
/// keeps re-checking <c>forge.check_pr</c> against zombie ids.
///
/// <para>The janitor is intentionally policy-light: it doesn't try to apply or
/// reject; it only marks stale. Operators can re-evaluate stale items via
/// <c>nexo background-agent proposals list --status Stale</c> and either
/// re-approve them (by manually moving the file) or delete them.</para>
/// </summary>
public sealed class ProposalJanitor
{
    /// <summary>Default age after which a Proposed item is considered stale.</summary>
    public static readonly TimeSpan DefaultProposedTtl = TimeSpan.FromHours(72);

    /// <summary>Default age after which an Approved-but-not-applied item is considered stale.</summary>
    public static readonly TimeSpan DefaultApprovedTtl = TimeSpan.FromHours(24);

    private readonly IChangeProposalStore _store;
    private readonly TimeSpan _proposedTtl;
    private readonly TimeSpan _approvedTtl;
    private readonly Func<DateTimeOffset> _now;

    public ProposalJanitor(
        IChangeProposalStore store,
        TimeSpan? proposedTtl = null,
        TimeSpan? approvedTtl = null,
        Func<DateTimeOffset>? now = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _proposedTtl = proposedTtl ?? ReadEnvSpan("NEXO_FORGE_PROPOSED_TTL_HOURS", DefaultProposedTtl);
        _approvedTtl = approvedTtl ?? ReadEnvSpan("NEXO_FORGE_APPROVED_TTL_HOURS", DefaultApprovedTtl);
        _now = now ?? (() => DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Sweep the queue once and stale anything past its TTL. Returns the ids
    /// the janitor moved (useful for CLI output and for tests).
    /// </summary>
    public IReadOnlyList<string> Sweep()
    {
        var staled = new List<string>();
        var now = _now();
        foreach (var p in _store.List(ChangeProposalStatus.Proposed))
        {
            if (now - p.UpdatedAt >= _proposedTtl)
            {
                _store.MarkStale(p.Id, note: $"auto-staled by janitor: proposed > {_proposedTtl.TotalHours:F1}h");
                staled.Add(p.Id);
            }
        }
        foreach (var p in _store.List(ChangeProposalStatus.Approved))
        {
            if (now - p.UpdatedAt >= _approvedTtl)
            {
                _store.MarkStale(p.Id, note: $"auto-staled by janitor: approved > {_approvedTtl.TotalHours:F1}h");
                staled.Add(p.Id);
            }
        }
        return staled;
    }

    private static TimeSpan ReadEnvSpan(string envVar, TimeSpan fallback)
    {
        var raw = Environment.GetEnvironmentVariable(envVar);
        if (string.IsNullOrWhiteSpace(raw)) return fallback;
        return double.TryParse(raw.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var hours) && hours > 0
            ? TimeSpan.FromHours(hours)
            : fallback;
    }
}
