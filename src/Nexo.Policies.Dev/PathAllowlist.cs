using System.Text.Json;
using Nexo.Abstractions;

namespace Nexo.Policies.Dev;

public sealed class PathAllowlist : IPolicy
{
    private static readonly string[] Allowed = { "src/", "tests/" };

    public bool Approve(ToolCall call, WorldSnapshot s, out string reason)
    {
        reason = "OK";
        if (call.Id is "repo.fs.write" or "repo.fs.search_replace")
        {
            if (call.Arguments.ValueKind == JsonValueKind.Object &&
                call.Arguments.TryGetProperty("path", out var p))
            {
                var rel = (p.GetString() ?? "").Replace('\\','/').TrimStart('/');
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
