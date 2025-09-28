using Nexo.Abstractions;
using System.Text.Json;

namespace Nexo.Tools.Assembly;

public sealed class AssemblySecurityScanTool : ITool
{
    public string Id => "assembly.security_scan";
    public ToolSchema Schema => new(Id, "Run a stub security scan", """
    {"type":"object","required":["path"],"properties":{"path":{"type":"string"}}}
    """);

    private sealed record Args(string path);

    public Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<Args>(call.Arguments)!;
        var findings = Array.Empty<object>();
        var payload = new { Findings = findings, Count = findings.Length };
        var delta = new ActionDelta(s.Tick, s.Tick + 1, new[] { $"security_scan:{args.path} findings={findings.Length}" });
        return Task.FromResult(new ToolResult(delta, payload));
    }
}
