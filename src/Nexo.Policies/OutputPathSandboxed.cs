using System.Text.Json;
using Nexo.Abstractions;

namespace Nexo.Policies;

public sealed class OutputPathSandboxed : IPolicy
{
    public bool Approve(ToolCall call, WorldSnapshot s, out string reason)
    {
        reason = "OK";
        if (!s.Data.TryGetValue("OutputRoot", out var rootObj) || rootObj is not string root)
            return true;

        if (call.Arguments.ValueKind != JsonValueKind.Object)
            return true;

        if (call.Arguments.TryGetProperty("output", out var outProp))
        {
            var output = outProp.GetString() ?? "";
            try
            {
                var full = Path.GetFullPath(output);
                var baseDir = Path.GetFullPath(root);
                if (!full.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                {
                    reason = $"Output '{full}' escapes sandbox '{baseDir}'";
                    return false;
                }
            }
            catch
            {
                reason = $"Invalid output path";
                return false;
            }
        }
        return true;
    }
}
