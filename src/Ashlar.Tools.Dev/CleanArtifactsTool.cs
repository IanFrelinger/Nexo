using System.Text.Json;
using Ashlar.Abstractions;
using Ashlar.Core.Application.Maintenance.Models;
using Ashlar.Core.Application.Maintenance.Ports;
using Ashlar.Tools.Dev.Deltas;

namespace Ashlar.Tools.Dev;

/// <summary>
/// Tool for artifact cleanup. Invokes IArtifactCleanupService with optional strategy and repo root.
/// Strategy IDs: test-artifacts, incomplete-blobs.
/// </summary>
public sealed class CleanArtifactsTool : ITool
{
    public const string IdConstant = "maintenance.clean_artifacts";

    private readonly IArtifactCleanupService _cleanupService;

    public CleanArtifactsTool(IArtifactCleanupService cleanupService)
    {
        _cleanupService = cleanupService ?? throw new ArgumentNullException(nameof(cleanupService));
    }

    public string Id => IdConstant;
    public ToolSchema Schema => new(Id, "Clean disk artifacts (test-artifacts, incomplete-blobs). Use strategyId to target a specific strategy.", """
    {"type":"object","properties":{"strategyId":{"type":"string","description":"Strategy ID (test-artifacts, incomplete-blobs) or omit for all"}}}
    """);

    // No `repoRoot`. It used to be accepted and PREFERRED over the snapshot
    // (`args.repoRoot ?? snapshot`), which let a model aim a delete-everything-under-here
    // operation anywhere on disk. This tool removes files; it is the highest-consequence
    // member of the family. See ToolSandbox.
    private sealed record Args(string? strategyId);

    public async Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct)
    {
        Args args;
        try
        {
            args = JsonSerializer.Deserialize<Args>(call.Arguments) ?? new Args(null);
        }
        catch
        {
            args = new Args(null);
        }

        if (!ToolSandbox.TryResolveRoot(s, out var repoRoot, out var reason))
        {
            var rejected = new RepoDelta { TickFrom = s.Tick, TickTo = s.Tick + 1 };
            rejected.AddLog($"clean_artifacts {reason}");
            return new ToolResult(rejected, new { cleaned = false, error = reason });
        }

        var context = new ArtifactCleanupContext(RepoRoot: repoRoot);

        var result = await _cleanupService.CleanAsync(args.strategyId, context, ct).ConfigureAwait(false);

        var delta = new RepoDelta { TickFrom = s.Tick, TickTo = s.Tick + 1 };
        delta.AddLog($"clean_artifacts:{result.StrategyId}:reclaimed={result.BytesReclaimed}");

        return new ToolResult(delta, new
        {
            strategyId = result.StrategyId,
            bytesReclaimed = result.BytesReclaimed,
            pathsDeleted = result.PathsDeleted.Count,
            errors = result.Errors
        });
    }
}
