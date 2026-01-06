using System.Text;
using Nexo.Abstractions;
using Nexo.Tools.Dev.Deltas;

namespace Nexo.Tools.Dev;

/// <summary>
/// Tool for writing files to the repository filesystem.
/// 
/// Writes content to a file at the specified path relative to the repository root.
/// Creates parent directories if they don't exist.
/// Tracks file edits in the action delta with SHA1 hashes and line counts.
/// 
/// Implements ITool for use with agent tool execution.
/// </summary>
public sealed class RepoFsWriteTool : ITool
{
    public string Id => "repo.fs.write";
    public ToolSchema Schema => new(Id, "Write a file under the repo root", """
    {"type":"object","required":["root","path","content"],"properties":{"root":{"type":"string"},"path":{"type":"string"},"content":{"type":"string"}}}
    """);

    private sealed record Args(string root, string path, string content);

    public async Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct)
    {
        var args = System.Text.Json.JsonSerializer.Deserialize<Args>(call.Arguments)!;
        var full = Path.Combine(args.root, args.path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        var before = File.Exists(full) ? await File.ReadAllTextAsync(full, ct) : "";
        await File.WriteAllTextAsync(full, args.content, new UTF8Encoding(false), ct);
        var after = await File.ReadAllTextAsync(full, ct);

        var delta = new RepoDelta { TickFrom = s.Tick, TickTo = s.Tick + 1 };
        delta.AddLog($"write:{args.path} bytes={args.content.Length}");
        delta.AddEdit(new FileEdit(args.path, Sha1(before), Sha1(after), CountPlus(after, before), CountMinus(after, before)));
        return new ToolResult(delta, new { path = args.path, bytes = args.content.Length });
    }

    private static string Sha1(string s)
    {
        using var sha = System.Security.Cryptography.SHA1.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(s)));
    }

    private static int CountPlus(string a, string b) => Math.Max(0, a.Length - b.Length);
    private static int CountMinus(string a, string b) => Math.Max(0, b.Length - a.Length);
}
