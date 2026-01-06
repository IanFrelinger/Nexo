using System.Text.Json;
using Nexo.Abstractions;

namespace Nexo.Policies.Dev;

/// <summary>
/// Development policy that limits the maximum size of file writes.
/// 
/// Prevents tool calls from writing files larger than the specified limit (default: 200KB).
/// Helps prevent accidental large file writes that could impact performance.
/// 
/// Implements IPolicy for use with PolicyEngine.
/// Checks "repo.fs.write" tool calls for content size.
/// </summary>
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
