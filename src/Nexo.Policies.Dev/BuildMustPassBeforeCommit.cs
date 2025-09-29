using Nexo.Abstractions;

namespace Nexo.Policies.Dev;

public sealed class BuildMustPassBeforeCommit : IPolicy
{
    public bool Approve(ToolCall call, WorldSnapshot s, out string reason)
    {
        reason = "OK";
        if (call.Id == "repo.git.commit")
        {
            var okBuild = s.Data.TryGetValue("LastBuildOk", out var b) && b is true;
            var okTest  = s.Data.TryGetValue("LastTestsOk", out var t) && t is true;
            if (!(okBuild && okTest)) { reason = "Cannot commit: build/tests not green"; return false; }
        }
        return true;
    }
}
