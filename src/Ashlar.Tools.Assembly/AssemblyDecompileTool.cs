using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using Ashlar.Abstractions;
using System.Text.Json;

namespace Ashlar.Tools.Assembly;

/// <summary>
/// Tool for decompiling .NET assemblies using ICSharpCode.Decompiler.
/// Writes C# source to the output directory.
/// </summary>
public sealed class AssemblyDecompileTool : ITool
{
    public string Id => "assembly.decompile";
    public ToolSchema Schema => new(Id, "Decompile .NET assembly to C# source", """
    {"type":"object","required":["path","output"],"properties":{"path":{"type":"string","description":"Path to .NET assembly"},"output":{"type":"string","description":"Output directory for decompiled source"}}}
    """);

    private sealed record Args(string path, string output);

    public async Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<Args>(call.Arguments)!;
        ct.ThrowIfCancellationRequested();

        if (!File.Exists(args.path))
        {
            var delta = new ActionDelta(s.Tick, s.Tick + 1, new[] { $"decompile:FAILED - file not found: {args.path}" });
            return new ToolResult(delta, new { Error = $"Assembly not found: {args.path}" });
        }

        Directory.CreateDirectory(args.output);
        var assemblyName = Path.GetFileNameWithoutExtension(args.path);
        var outFile = Path.Combine(args.output, $"{assemblyName}.decompiled.cs");

        try
        {
            var decompiler = new CSharpDecompiler(args.path, new DecompilerSettings
            {
                ThrowOnAssemblyResolveErrors = false
            });
            var sourceCode = decompiler.DecompileWholeModuleAsString();
            await File.WriteAllTextAsync(outFile, sourceCode, ct);
        }
        catch (Exception ex)
        {
            var stubContent = $@"// Decompilation failed: {ex.Message}
// Assembly: {args.path}
// Error: {ex.GetType().Name}
";
            await File.WriteAllTextAsync(outFile, stubContent, ct);
        }

        var successDelta = new ActionDelta(s.Tick, s.Tick + 1, new[] { $"decompile:{args.path} -> {outFile}" });
        return new ToolResult(successDelta, new { Output = outFile });
    }
}
