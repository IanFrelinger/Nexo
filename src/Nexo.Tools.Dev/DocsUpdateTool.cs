using System.Text;
using Nexo.Abstractions;
using Nexo.Tools.Dev.Deltas;

namespace Nexo.Tools.Dev;

/// <summary>
/// Tool for updating documentation files.
/// 
/// Appends entries to CHANGELOG.md in the repository root.
/// Each entry is timestamped and formatted as a changelog item.
/// 
/// Implements ITool for use with agent tool execution.
/// </summary>
public sealed class DocsUpdateTool : ITool
{
    public string Id => "docs.update";
    public ToolSchema Schema => new(Id, "Append to CHANGELOG.md", """
    {"type":"object","required":["root","entry"],"properties":{"root":{"type":"string"},"entry":{"type":"string"}}}
    """);

    private sealed record Args(string root, string entry);

    public async Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct)
    {
        var args = System.Text.Json.JsonSerializer.Deserialize<Args>(call.Arguments)!;
        var path = Path.Combine(args.root, "CHANGELOG.md");
        await File.AppendAllTextAsync(path, $"- {DateTimeOffset.UtcNow:u} {args.entry}\n", new UTF8Encoding(false), ct);

        var delta = new RepoDelta { TickFrom = s.Tick, TickTo = s.Tick + 1 };
        delta.AddLog("docs:update");
        return new ToolResult(delta, new { path });
    }
}
