using Nexo.Abstractions;

namespace Nexo.Policies;

public sealed class AllowAllPolicy : IPolicy
{
    public bool Approve(ToolCall call, WorldSnapshot s, out string reason)
    {
        reason = "OK";
        return true;
    }
}
