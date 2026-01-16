using Nexo.Abstractions;
using Nexo.Tools.Dev.Deltas;

namespace Nexo.Tools.Dev;

/// <summary>
/// Tool for building .NET projects using dotnet CLI.
/// 
/// Executes `dotnet build -c Release` in the specified root directory.
/// Returns build exit code, stdout, and stderr in the tool result.
/// 
/// Implements ITool for use with agent tool execution.
/// </summary>
public sealed class DotnetBuildTool : ITool
{
    public string Id => "dotnet.build";
    public ToolSchema Schema => new(Id, "Run dotnet build -c Release", """
    {"type":"object","required":["root"],"properties":{"root":{"type":"string"}}}
    """);

    private sealed record Args(string root);

    public async Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct)
    {
        var args = System.Text.Json.JsonSerializer.Deserialize<Args>(call.Arguments)!;
        var (code, stdout, stderr, timedOut) = await DotnetRunner.RunAsync(
            args.root,
            "build -c Release",
            timeout: TimeSpan.FromMinutes(10),
            ct);
        var delta = new RepoDelta { TickFrom = s.Tick, TickTo = s.Tick + 1 };
        delta.AddLog($"build:exit={code}");
        if (timedOut) delta.AddLog("build:timeout");
        if (!string.IsNullOrWhiteSpace(stderr)) delta.AddLog("build:stderr");
        return new ToolResult(delta, new { ok = code == 0, stdout, stderr });
    }
}
