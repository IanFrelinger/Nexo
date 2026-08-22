using System.Collections.Concurrent;
using Ashlar.Core.Domain;

namespace Ashlar.BackgroundAgents.Logging;

/// <summary>
/// In-memory log store with a bounded buffer per agent.
/// </summary>
public sealed class InMemoryAgentLogStore : IBackgroundAgentLogStore
{
    private const int DefaultMaxEntriesPerAgent = AshlarDefaults.AgentLogMaxEntriesPerAgent;
    private readonly ConcurrentDictionary<string, BoundedLogBuffer> _buffers = new(StringComparer.OrdinalIgnoreCase);
    private readonly int _maxEntriesPerAgent;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryAgentLogStore"/> class.
    /// </summary>
    /// <param name="maxEntriesPerAgent">Maximum entries to keep per agent (default 1000).</param>
    public InMemoryAgentLogStore(int maxEntriesPerAgent = DefaultMaxEntriesPerAgent)
    {
        _maxEntriesPerAgent = maxEntriesPerAgent > 0 ? maxEntriesPerAgent : DefaultMaxEntriesPerAgent;
    }

    /// <inheritdoc />
    public void Append(string agentId, string level, string message)
    {
        if (string.IsNullOrEmpty(agentId))
            return;
        var buffer = _buffers.GetOrAdd(agentId, _ => new BoundedLogBuffer(_maxEntriesPerAgent));
        buffer.Add(new AgentLogEntry(agentId, DateTimeOffset.UtcNow, level ?? "Info", message ?? string.Empty));
    }

    /// <inheritdoc />
    public IReadOnlyList<AgentLogEntry> GetRecent(string agentId, int maxCount = 100, string? levelFilter = null, DateTimeOffset? since = null)
    {
        if (string.IsNullOrEmpty(agentId) || !_buffers.TryGetValue(agentId, out var buffer))
            return Array.Empty<AgentLogEntry>();
        return buffer.GetRecent(maxCount, levelFilter, since);
    }

    private sealed class BoundedLogBuffer
    {
        private readonly int _capacity;
        private readonly List<AgentLogEntry> _entries = new();
        private readonly object _lock = new();

        internal BoundedLogBuffer(int capacity)
        {
            _capacity = capacity;
        }

        internal void Add(AgentLogEntry entry)
        {
            lock (_lock)
            {
                _entries.Add(entry);
                if (_entries.Count > _capacity)
                    _entries.RemoveRange(0, _entries.Count - _capacity);
            }
        }

        internal IReadOnlyList<AgentLogEntry> GetRecent(int maxCount, string? levelFilter, DateTimeOffset? since)
        {
            lock (_lock)
            {
                var list = _entries.AsEnumerable();
                if (since.HasValue)
                    list = list.Where(e => e.Timestamp >= since.Value);
                if (!string.IsNullOrEmpty(levelFilter))
                    list = list.Where(e => string.Equals(e.Level, levelFilter, StringComparison.OrdinalIgnoreCase));
                return list.TakeLast(maxCount).ToList();
            }
        }
    }
}
