using System.Diagnostics;
using Nexo.Abstractions;
using Nexo.Tools.Dev.Deltas;

namespace Nexo.Tools.Dev;

public sealed class DotnetTestTool : ITool
{
    public string Id => "dotnet.test";
    public ToolSchema Schema => new(Id, "Run dotnet test --no-build --logger trx", """
    {"type":"object","required":["root"],"properties":{"root":{"type":"string"}}}
    """);

    private sealed record Args(string root);

    public async Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct)
    {
        var args = System.Text.Json.JsonSerializer.Deserialize<Args>(call.Arguments)!;
        var (code, stdout, stderr) = await Run("dotnet", "test --logger trx --no-build", args.root, ct);
        var delta = new RepoDelta { TickFrom = s.Tick, TickTo = s.Tick + 1 };
        delta.AddLog($"test:exit={code}");
        if (!string.IsNullOrWhiteSpace(stderr)) delta.AddLog("test:stderr");
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
