using Nexo.Abstractions;

namespace Nexo.Runtime;

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

    public void Write(EventRecord e) => _events.Add(e);

    public IReadOnlyList<EventRecord> Query(string filter, int k)
    {
        return _events
            .Where(e => e.Message.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.At)
            .Take(k)
            .ToList();
    }
}
