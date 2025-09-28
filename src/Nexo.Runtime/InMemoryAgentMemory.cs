using Nexo.Abstractions;

namespace Nexo.Runtime;

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
