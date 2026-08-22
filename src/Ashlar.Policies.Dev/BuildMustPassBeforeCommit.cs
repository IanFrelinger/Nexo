using Ashlar.Abstractions;

namespace Ashlar.Policies.Dev;

/// <summary>
/// Development policy that requires builds and tests to pass before allowing commits.
/// 
/// Prevents commits when:
/// - Last build was not successful
/// - Last test run was not successful
/// 
/// Implements IPolicy for use with PolicyEngine.
/// Used in development environments to enforce quality gates.
/// </summary>
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
