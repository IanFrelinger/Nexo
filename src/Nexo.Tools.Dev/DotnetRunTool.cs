using Nexo.Abstractions;
using Nexo.Tools.Dev.Deltas;

namespace Nexo.Tools.Dev;

/// <summary>
/// Tool for running arbitrary `dotnet` commands in a directory.
/// This is the "wrapped dependency" for process execution used by demos/pipelines.
/// </summary>
public sealed class DotnetRunTool : ITool
{
    public string Id => "dotnet.run";

    public ToolSchema Schema => new(Id, "Run dotnet <args> in a working directory", """
    {
      "type":"object",
      "required":["root","args"],
      "properties":{
        "root":{"type":"string"},
        "args":{"type":"string"},
        "timeoutSeconds":{"type":"integer"}
      }
    }
    """);

    private sealed record Args(string root, string args, int? timeoutSeconds);

    public async Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct)
    {
        var args = System.Text.Json.JsonSerializer.Deserialize<Args>(call.Arguments)!;
        var timeout = TimeSpan.FromSeconds(args.timeoutSeconds ?? 300);

        var (code, stdout, stderr, timedOut) = await DotnetRunner.RunAsync(
            args.root,
            args.args,
            timeout,
            ct);

        var delta = new RepoDelta { TickFrom = s.Tick, TickTo = s.Tick + 1 };
        delta.AddLog($"dotnet:exit={code}");
        if (timedOut) delta.AddLog("dotnet:timeout");
        if (!string.IsNullOrWhiteSpace(stderr)) delta.AddLog("dotnet:stderr");

        return new ToolResult(delta, new { ok = code == 0, exitCode = code, stdout, stderr, timedOut });
    }
}

