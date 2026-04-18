using System.Text.Json;
using Nexo.Abstractions;
using Nexo.Tools.Dev.Deltas;

namespace Nexo.Tools.Dev;

/// <summary>
/// Forge-branded build probe: same <c>dotnet build -c Release</c> as <see cref="DotnetBuildTool"/>,
/// registered alongside the forge proposal tools so planners can verify the tree after
/// <c>forge.propose_change</c> / operator apply without conflating the capability with
/// generic <c>dotnet.build</c> in prompts. Shares the same per-cycle build budget as <c>dotnet.build</c>.
/// </summary>
public sealed class ForgeBuildTool : ITool
{
    public string Id => "forge.build";

    public ToolSchema Schema => new(Id,
        "Run dotnet build -c Release to verify the repo after forge proposal work (same as dotnet.build)",
        """
        {"type":"object","required":["root"],"properties":{"root":{"type":"string","description":"Working directory passed to dotnet (usually RepoRoot)"}}}
        """);

    private sealed record Args(string root);

    public async Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<Args>(call.Arguments)
                   ?? throw new InvalidOperationException("forge.build: empty arguments");
        var (code, stdout, stderr, timedOut) = await DotnetBuildTool.RunReleaseBuildAsync(args.root, ct)
            .ConfigureAwait(false);
        var delta = new RepoDelta { TickFrom = s.Tick, TickTo = s.Tick + 1 };
        delta.AddLog($"forge.build:exit={code}");
        if (timedOut) delta.AddLog("forge.build:timeout");
        if (!string.IsNullOrWhiteSpace(stderr)) delta.AddLog("forge.build:stderr");
        return new ToolResult(delta, new { ok = code == 0, stdout, stderr });
    }
}
