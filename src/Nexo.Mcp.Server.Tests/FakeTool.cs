using System.Text.Json;
using Nexo.Abstractions;

namespace Nexo.Mcp.Server.Tests;

/// <summary>Configurable ITool double shared by the contributor and bridge tests.</summary>
internal sealed class FakeTool : ITool
{
    private readonly Func<ToolCall, WorldSnapshot, CancellationToken, Task<ToolResult>> _invoke;

    public FakeTool(
        string id,
        string? inputJsonSchema = """{"type":"object"}""",
        Func<ToolCall, WorldSnapshot, CancellationToken, Task<ToolResult>>? invoke = null,
        string description = "fake tool")
    {
        Id = id;
        Schema = new ToolSchema(id, description, inputJsonSchema);
        _invoke = invoke ?? ((_, _, _) => Task.FromResult(new ToolResult(new FakeDelta(), "ok")));
    }

    public string Id { get; }

    public ToolSchema Schema { get; }

    public ToolCall? LastCall { get; private set; }

    public WorldSnapshot? LastSnapshot { get; private set; }

    public Task<ToolResult> InvokeAsync(ToolCall toolCall, WorldSnapshot s, CancellationToken ct)
    {
        LastCall = toolCall;
        LastSnapshot = s;
        return _invoke(toolCall, s, ct);
    }
}

/// <summary>Minimal IActionDelta for tool results in tests.</summary>
internal sealed class FakeDelta : IActionDelta
{
    private readonly List<string> _log = new();

    public int TickFrom => 0;

    public int TickTo => 1;

    public IReadOnlyList<byte>? Signature { get; set; }

    public IReadOnlyList<string> Log => _log;

    public FakeDelta WithLog(string line)
    {
        _log.Add(line);
        return this;
    }
}

internal static class Json
{
    public static JsonElement Object(string json = "{}")
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }
}
