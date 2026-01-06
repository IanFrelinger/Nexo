using Nexo.Abstractions;

namespace Nexo.Policies;

/// <summary>
/// Policy that enforces performance headroom limits (placeholder implementation).
/// 
/// Intended to limit tool execution time to prevent resource exhaustion.
/// Currently a placeholder - future implementation would track execution time.
/// 
/// Implements IPolicy for use with PolicyEngine.
/// </summary>
public sealed class PerfHeadroom : IPolicy
{
    private readonly TimeSpan _maxPerTool;
    public PerfHeadroom(TimeSpan maxPerTool) => _maxPerTool = maxPerTool;

    public bool Approve(ToolCall call, WorldSnapshot s, out string reason)
    {
        // Placeholder: implement timing budget around tool execution if needed.
        reason = "OK";
        return true;
    }
}
