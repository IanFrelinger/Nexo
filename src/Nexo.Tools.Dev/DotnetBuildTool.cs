using System.Diagnostics;
using Nexo.Abstractions;
using Nexo.Tools.Dev.Deltas;
using System.Text;

namespace Nexo.Tools.Dev;

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
        var (code, stdout, stderr) = await Run("dotnet", "build -c Release", args.root, ct);
        var delta = new RepoDelta { TickFrom = s.Tick, TickTo = s.Tick + 1 };
        delta.AddLog($"build:exit={code}");
        if (!string.IsNullOrWhiteSpace(stderr)) delta.AddLog("build:stderr");
        return new ToolResult(delta, new { ok = code == 0, stdout, stderr });
    }

    private static async Task<(int, string, string)> Run(string file, string args, string cwd, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(file, args) { WorkingDirectory = cwd, RedirectStandardOutput = true, RedirectStandardError = true };
        using var p = Process.Start(psi)!;
        var so = await p.StandardOutput.ReadToEndAsync(ct);
        var se = await p.StandardError.ReadToEndAsync(ct);
        await p.WaitForExitAsync(ct);
        return (p.ExitCode, so, se);
    }
}
