using Nexo.Abstractions;
using System.Reflection;
using System.Text.Json;

namespace Nexo.Tools.Assembly;

public sealed class AssemblyAnalyzeTool : ITool
{
    public string Id => "assembly.analyze";
    public ToolSchema Schema => new(Id, "Analyze a .NET assembly for basic metadata", """
    {"type":"object","required":["path"],"properties":{"path":{"type":"string"}}}
    """);

    private sealed record Args(string path);

    public Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<Args>(call.Arguments)!;
        var assemblyName = AssemblyName.GetAssemblyName(args.path);
        var payload = new
        {
            assemblyName.Name,
            assemblyName.Version,
            CultureName = assemblyName.CultureName ?? "",
            Flags = assemblyName.Flags.ToString()
        };
        var delta = new ActionDelta(s.Tick, s.Tick + 1, new[] { $"analyze:{assemblyName.Name} v{assemblyName.Version}" });
        return Task.FromResult(new ToolResult(delta, payload));
    }
}
