using System.Collections.Concurrent;

namespace Ashlar.AI.Pipeline.Governance;

/// <summary>
/// In-memory auditor for tests and hosts that have not wired trust audit yet.
/// </summary>
public sealed class InMemoryChatInvocationAuditor : IChatInvocationAuditor
{
    private readonly ConcurrentQueue<ChatInvocationAuditRecord> _records = new();

    /// <summary>Snapshot of recorded audits (newest last).</summary>
    public IReadOnlyList<ChatInvocationAuditRecord> Records => _records.ToArray();

    /// <inheritdoc />
    public void Record(ChatInvocationAuditRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        _records.Enqueue(record);
    }
}
