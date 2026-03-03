using System.IO;
using System.Text.Json;
using Nexo.Abstractions;

namespace Nexo.Policies.Dev;

/// <summary>
/// Development policy that restricts file operations to allowlisted paths.
/// 
/// Only allows file writes and search/replace operations in:
/// - src/ directory
/// - tests/ directory
/// 
/// Prevents modifications to other parts of the codebase.
/// Implements IPolicy for use with PolicyEngine.
/// </summary>
public sealed class PathAllowlist : IPolicy
{
    private static readonly string[] Allowed = { "src/", "tests/", "docs/" };

    public bool Approve(ToolCall call, WorldSnapshot s, out string reason)
    {
        reason = "OK";
        if (call.Id is "repo.fs.write" or "repo.fs.search_replace")
        {
            if (call.Arguments.ValueKind == JsonValueKind.Object &&
                call.Arguments.TryGetProperty("path", out var p))
            {
                var raw = (p.ValueKind == JsonValueKind.Null ? "" : (p.GetString() ?? "")).Replace('\\', '/');
                var rel = raw.TrimStart('/');
                if (string.IsNullOrEmpty(rel))
                {
                    reason = "Path not allowed: empty or null path";
                    return false;
                }
                if (Path.IsPathRooted(raw) || raw.StartsWith("/", StringComparison.Ordinal))
                {
                    reason = $"Path not allowed: absolute path not permitted: {raw}";
                    return false;
                }
                if (rel.Contains("..", StringComparison.Ordinal))
                {
                    reason = $"Path not allowed: path traversal not permitted: {rel}";
                    return false;
                }
                if (!Allowed.Any(a => rel.StartsWith(a, StringComparison.OrdinalIgnoreCase)))
                {
                    reason = $"Path not allowed: {rel}";
                    return false;
                }
            }
        }
        return true;
    }
}
