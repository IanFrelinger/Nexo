using Nexo.Abstractions;
using Nexo.Tools.Dev.Deltas;

namespace Nexo.Tools.Dev;

/// <summary>
/// Tool for running .NET tests using dotnet CLI.
///
/// Executes `dotnet test --logger trx --no-build --blame-hang-timeout 60s --blame-hang-dump-type none --verbosity minimal`
/// in the specified root directory. Blame-hang safeguards prevent 6GB+ hang dumps; minimal verbosity reduces output buffering.
/// Returns test exit code, stdout, and stderr in the tool result.
///
/// Implements ITool for use with agent tool execution.
/// </summary>
public sealed class DotnetTestTool : ITool
{
    public string Id => "dotnet.test";
    public ToolSchema Schema => new(Id, "Run dotnet test --no-build --logger trx --blame-hang-timeout 60s --blame-hang-dump-type none", """
    {"type":"object","required":["root"],"properties":{"root":{"type":"string"}}}
    """);

    private sealed record Args(string root);

    public async Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct)
    {
        var args = System.Text.Json.JsonSerializer.Deserialize<Args>(call.Arguments)!;
        var (code, stdout, stderr, timedOut) = await DotnetRunner.RunAsync(
            args.root,
            "test --logger trx --no-build --blame-hang-timeout 60s --blame-hang-dump-type none --verbosity minimal",
            timeout: TimeSpan.FromMinutes(20),
            ct);
        var delta = new RepoDelta { TickFrom = s.Tick, TickTo = s.Tick + 1 };
        delta.AddLog($"test:exit={code}");
        if (timedOut) delta.AddLog("test:timeout");
        if (!string.IsNullOrWhiteSpace(stderr)) delta.AddLog("test:stderr");
        return new ToolResult(delta, new { ok = code == 0, stdout, stderr });
    }
}
