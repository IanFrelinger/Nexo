using Ashlar.Abstractions;

namespace Ashlar.Runtime;

/// <summary>
/// In-memory implementation of IAgentMemory.
/// 
/// Responsibilities:
/// - Stores event records in memory
/// - Provides query functionality with filtering
/// - Returns most recent events matching filter criteria
/// 
/// Implements IAgentMemory for agent event storage and retrieval.
/// Used by CapabilityRegistry to provide memory for agents.
/// </summary>
public sealed class InMemoryAgentMemory : IAgentMemory
{
    private readonly List<EventRecord> _events = new();

    /// <summary>Appends an event record to agent memory.</summary>
    public void Write(EventRecord e) => _events.Add(e);

    /// <summary>Returns up to <paramref name="k"/> recent events matching <paramref name="filter"/>.</summary>
    public IReadOnlyList<EventRecord> Query(string filter, int k)
    {
        return _events
            .Where(e => e.Message != null && e.Message.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
            .OrderByDescending(e => e.At)
            .Take(k)
            .ToList();
    }
}
