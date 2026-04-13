using System.Collections.Concurrent;
using Nexo.Core.Domain;

namespace Nexo.BackgroundAgents.Trust;

/// <summary>
/// In-memory implementation of ISanitizationAuditLog.
/// </summary>
public sealed class InMemorySanitizationAuditLog : ISanitizationAuditLog
{
    private readonly ConcurrentQueue<SanitizationAuditEntry> _entries = new();
    private const int MaxEntries = NexoDefaults.SanitizationAuditMaxEntries;

    /// <inheritdoc />
    public void LogRedaction(DateTimeOffset timestamp, string ruleVersion, string fieldOrType, string disposition, string? reason)
    {
        var entry = new SanitizationAuditEntry
        {
            Timestamp = timestamp,
            RuleVersion = ruleVersion,
            FieldOrType = fieldOrType,
            Disposition = disposition,
            Reason = reason,
        };
        _entries.Enqueue(entry);
        while (_entries.Count > MaxEntries && _entries.TryDequeue(out _)) { }
    }

    /// <inheritdoc />
    public IReadOnlyList<SanitizationAuditEntry> GetRecent(int maxCount, DateTimeOffset? since = null)
    {
        var list = _entries.ToArray();
        var filtered = since.HasValue
            ? list.Where(e => e.Timestamp >= since.Value).ToArray()
            : list;
        return filtered
            .OrderByDescending(e => e.Timestamp)
            .Take(maxCount)
            .ToList();
    }
}
