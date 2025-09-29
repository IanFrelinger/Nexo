using Nexo.Abstractions;
using Nexo.Tools.Dev.Deltas;

namespace Nexo.Tools.Dev;

public sealed class RepoGitCommitTool : ITool
{
    public string Id => "repo.git.commit";
    public ToolSchema Schema => new(Id, "Record a pseudo-commit (demo)", """
    {"type":"object","required":["message","root"],"properties":{"message":{"type":"string"},"root":{"type":"string"}}}
    """);

    private sealed record Args(string message, string root);

    public async Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct)
    {
        var args = System.Text.Json.JsonSerializer.Deserialize<Args>(call.Arguments)!;
        var logPath = Path.Combine(args.root, "COMMIT_LOG.txt");
        await File.AppendAllTextAsync(logPath, $"[{DateTimeOffset.UtcNow:u}] {args.message}\n");
        var delta = new RepoDelta { TickFrom = s.Tick, TickTo = s.Tick + 1 };
        delta.AddLog($"commit:{args.message}");
        return new ToolResult(delta, new { path = logPath });
    }
}
