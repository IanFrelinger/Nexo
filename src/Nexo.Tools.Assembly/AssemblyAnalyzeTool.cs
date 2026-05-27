using Mono.Cecil;
using Nexo.Abstractions;
using System.Reflection;
using System.Text.Json;

namespace Nexo.Tools.Assembly;

/// <summary>
/// Tool for analyzing .NET assemblies.
/// 
/// Provides:
/// - Assembly metadata extraction (name, version, culture, flags)
/// - Basic assembly information retrieval
/// 
/// Implements ITool for use with agent toolboxes.
/// Used by agents to analyze .NET assemblies.
/// </summary>
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
        var complexity = EstimateMethodCount(args.path);
        var payload = new
        {
            assemblyName.Name,
            assemblyName.Version,
            CultureName = assemblyName.CultureName ?? "",
            Flags = assemblyName.Flags.ToString(),
            Complexity = complexity
        };
        var delta = new ActionDelta(s.Tick, s.Tick + 1, new[] { $"analyze:{assemblyName.Name} v{assemblyName.Version}" });
        return Task.FromResult(new ToolResult(delta, payload));
    }

    private static int EstimateMethodCount(string path)
    {
        try
        {
            using var module = ModuleDefinition.ReadModule(path, new ReaderParameters { InMemory = true });
            return module.Types.Sum(type => type.Methods.Count(method => method.HasBody));
        }
        catch
        {
            return 0;
        }
    }
}
