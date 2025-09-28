using System.Text.Json;

namespace Nexo.Abstractions;

public interface IAgent
{
    string Name { get; }
    Task<AgentActions> ThinkAsync(AgentObservation obs, IToolbox tools, IAgentMemory mem, CancellationToken ct);
}

public interface ITool
{
    string Id { get; }
    ToolSchema Schema { get; }
    Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct);
}

public interface IPolicy
{
    bool Approve(ToolCall call, WorldSnapshot s, out string reason);
}

public interface IModel
{
    Task<ModelOutput> CompleteAsync(ModelInput input, CancellationToken ct);
}

public interface IActionDelta
{
    int TickFrom { get; }
    int TickTo { get; }
    byte[]? Signature { get; set; }
    IReadOnlyList<string> Log { get; }
}

public sealed record ActionDelta(int TickFrom, int TickTo, IReadOnlyList<string> Log) : IActionDelta
{
    public byte[]? Signature { get; set; }
    public static IActionDelta Merge(IEnumerable<IActionDelta> deltas)
    {
        var list = deltas.ToList();
        if (list.Count == 0) return new ActionDelta(0, 0, Array.Empty<string>());
        var from = list.Min(d => d.TickFrom);
        var to = list.Max(d => d.TickTo);
        var log = list.SelectMany(d => d.Log).ToList();
        return new ActionDelta(from, to, log);
    }
}

public sealed record ToolSchema(string Id, string Description, string? InputJsonSchema);

public sealed record ToolCall(string Id, JsonElement Arguments)
{
    public T ParseArgs<T>() => JsonSerializer.Deserialize<T>(Arguments)!;
}

public sealed record ToolResult(IActionDelta Delta, object? Payload);

public sealed record AgentActions(IReadOnlyList<ToolCall> ToolCalls)
{
    public static AgentActions None { get; } = new AgentActions(Array.Empty<ToolCall>());
}

public sealed record WorldSnapshot(int Tick, IReadOnlyDictionary<string, object?> Data);

public sealed record AgentObservation(WorldSnapshot Snapshot);

public sealed record EventRecord(DateTimeOffset At, string Agent, string EventType, string Message);

public interface IAgentMemory
{
    void Write(EventRecord e);
    IReadOnlyList<EventRecord> Query(string filter, int k);
}

public interface IToolbox
{
    IEnumerable<ToolSchema> Schemas();
    Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct);
    IAgentMemory MemoryFor(IAgent agent);
}

public sealed record ModelInput(IReadOnlyList<(string role, string content)> Messages);
public sealed record ModelOutput(string Text);
