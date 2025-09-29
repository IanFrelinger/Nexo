using System.Text;
using Nexo.Abstractions;
using Nexo.Tools.Dev.Deltas;

namespace Nexo.Tools.Dev;

public sealed class RepoFsEnsureFileTool : ITool
{
    public string Id => "repo.fs.ensure_file";
    public ToolSchema Schema => new(Id, "Create file with content if it doesn't exist", """
    {"type":"object","required":["root","path","content"],"properties":{"root":{"type":"string"},"path":{"type":"string"},"content":{"type":"string"}}}
    """);

    private sealed record Args(string root, string path, string content);

    public async Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct)
    {
        var args = System.Text.Json.JsonSerializer.Deserialize<Args>(call.Arguments)!;
        var full = Path.Combine(args.root, args.path);
        if (!File.Exists(full))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            await File.WriteAllTextAsync(full, args.content, new UTF8Encoding(false), ct);
            var delta = new RepoDelta { TickFrom = s.Tick, TickTo = s.Tick + 1 };
            delta.AddLog($"ensure:{args.path} created");
            delta.AddEdit(new FileEdit(args.path, null, Sha1(args.content), args.content.Length, 0));
            return new ToolResult(delta, new { created = true });
        }
        else
        {
            var delta = new RepoDelta { TickFrom = s.Tick, TickTo = s.Tick + 1 };
            delta.AddLog($"ensure:{args.path} exists");
            return new ToolResult(delta, new { created = false });
        }
    }

    private static string Sha1(string s)
    {
        using var sha = System.Security.Cryptography.SHA1.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(s)));
    }
}
