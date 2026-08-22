using Ashlar.Abstractions;

namespace Ashlar.Policies;

/// <summary>
/// Policy that allows all tool calls without restrictions.
/// 
/// Used for permissive environments where no restrictions are needed.
/// Always returns true for Approve() with reason "OK".
/// 
/// Implements IPolicy for use with PolicyEngine.
/// Useful for testing and development scenarios.
/// </summary>
public sealed class AllowAllPolicy : IPolicy
{
    public bool Approve(ToolCall call, WorldSnapshot s, out string reason)
    {
        reason = "OK";
        return true;
    }
}
