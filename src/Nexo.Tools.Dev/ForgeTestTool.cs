using System.Text.Json;
using Nexo.Abstractions;
using Nexo.Tools.Dev.Deltas;

namespace Nexo.Tools.Dev;

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
        {"type":"object","required":["root"],"properties":{"root":{"type":"string","description":"Working directory passed to dotnet (usually RepoRoot; must be built first)"}}}
        """);

    private sealed record Args(string root);

    public async Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<Args>(call.Arguments)
                   ?? throw new InvalidOperationException("forge.test: empty arguments");
        var (code, stdout, stderr, timedOut) = await DotnetTestTool.RunTrxTestsNoBuildAsync(args.root, ct)
            .ConfigureAwait(false);
        var delta = new RepoDelta { TickFrom = s.Tick, TickTo = s.Tick + 1 };
        delta.AddLog($"forge.test:exit={code}");
        if (timedOut) delta.AddLog("forge.test:timeout");
        if (!string.IsNullOrWhiteSpace(stderr)) delta.AddLog("forge.test:stderr");
        return new ToolResult(delta, new { ok = code == 0, stdout, stderr });
    }
}
