using System.Text.Json;
using Ashlar.Abstractions;
using Ashlar.Tools.Dev.Deltas;

namespace Ashlar.Tools.Dev;

/// <summary>
/// Forge-branded test probe: same TRX <c>dotnet test --no-build</c> invocation as
/// <see cref="DotnetTestTool"/>, for use when the forge proposal queue is enabled.
/// Shares the per-cycle test budget with <c>dotnet.test</c>.
/// </summary>
public sealed class ForgeTestTool : ITool
{
    public string Id => "forge.test";

    public ToolSchema Schema => new(Id,
        "Run dotnet test (TRX, --no-build) to verify tests after forge work (same as dotnet.test)",
        """
        {"type":"object","properties":{}}
        """);


    public async Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct)
    {
        // Working directory from the sandbox, not the model — see DotnetBuildTool.
        if (!ToolSandbox.TryResolveRoot(s, out var root, out var reason))
        {
            var rejected = new RepoDelta { TickFrom = s.Tick, TickTo = s.Tick + 1 };
            rejected.AddLog($"forge.test:{reason}");
            return new ToolResult(rejected, new { ok = false, error = reason });
        }

        var (code, stdout, stderr, timedOut) = await DotnetTestTool.RunTrxTestsNoBuildAsync(root, ct)
            .ConfigureAwait(false);
        var delta = new RepoDelta { TickFrom = s.Tick, TickTo = s.Tick + 1 };
        delta.AddLog($"forge.test:exit={code}");
        if (timedOut) delta.AddLog("forge.test:timeout");
        if (!string.IsNullOrWhiteSpace(stderr)) delta.AddLog("forge.test:stderr");
        return new ToolResult(delta, new { ok = DotnetTestTool.Succeeded(code, timedOut, stdout, stderr), stdout, stderr });
    }
}
