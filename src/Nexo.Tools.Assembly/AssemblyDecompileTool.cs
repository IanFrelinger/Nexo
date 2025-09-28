using Nexo.Abstractions;
using System.Text.Json;

namespace Nexo.Tools.Assembly;

public sealed class AssemblyDecompileTool : ITool
{
    public string Id => "assembly.decompile";
    public ToolSchema Schema => new(Id, "Stub decompilation - writes a placeholder .txt", """
    {"type":"object","required":["path","output"],"properties":{"path":{"type":"string"},"output":{"type":"string"}}}
    """);

    private sealed record Args(string path, string output);

    public async Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<Args>(call.Arguments)!;
        Directory.CreateDirectory(args.output);
        var outFile = Path.Combine(args.output, "DECOMPILE_STUB.txt");
        await File.WriteAllTextAsync(outFile,
            $"[stub] Decompilation not implemented.\nAssembly: {args.path}\nTick: {s.Tick}\n",
            ct);

        var delta = new ActionDelta(s.Tick, s.Tick + 1, new[] { $"decompile:{args.path} -> {outFile}" });
        var payload = new { Output = outFile };
        return new ToolResult(delta, payload);
    }
}
