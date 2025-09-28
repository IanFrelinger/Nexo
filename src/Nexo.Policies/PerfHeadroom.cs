using Nexo.Abstractions;

namespace Nexo.Policies;

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
