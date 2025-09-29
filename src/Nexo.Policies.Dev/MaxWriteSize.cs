using System.Text.Json;
using Nexo.Abstractions;

namespace Nexo.Policies.Dev;

public sealed class MaxWriteSize : IPolicy
{
    private readonly int _maxBytes;
    public MaxWriteSize(int maxBytes = 200_000) => _maxBytes = maxBytes;

    public bool Approve(ToolCall call, WorldSnapshot s, out string reason)
    {
        reason = "OK";
        if (call.Id is "repo.fs.write" && call.Arguments.ValueKind == JsonValueKind.Object)
        {
            if (call.Arguments.TryGetProperty("content", out var c))
            {
                var bytes = (c.GetString() ?? string.Empty).Length;
                if (bytes > _maxBytes) { reason = $"Write too large: {bytes} > max {_maxBytes}"; return false; }
            }
        }
        return true;
    }
}
