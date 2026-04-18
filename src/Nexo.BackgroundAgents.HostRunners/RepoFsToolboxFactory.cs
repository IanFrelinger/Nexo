using Nexo.Abstractions;
using Nexo.Policies.Dev;
using Nexo.Runtime;
using Nexo.Tools.Dev;

namespace Nexo.BackgroundAgents.HostRunners;

/// <summary>
/// Shared factory for the minimal repo-fs toolbox (read, list, write, search_replace) and the
/// matching policy (path allowlist, max write size). Used by SelfExtendRunnerAdapter to avoid
/// duplicating tool/policy setup.
///
/// <para>The toolbox includes <c>repo.fs.read</c> and <c>repo.fs.list</c> as well as the write
/// tools. Without read/list capabilities the LLM has nothing to inspect and rationally returns
/// no tool calls — leaving the planner agent silent in practice.</para>
/// </summary>
internal static class RepoFsToolboxFactory
{
    /// <summary>
    /// Creates a minimal toolbox with read/list (inspection) and write/search_replace
    /// (modification) tools, plus a policy engine with PathAllowlist and MaxWriteSize.
    /// PathAllowlist gates only the write/search_replace tools, so read/list operate freely
    /// inside the resolved repo root (each read/list tool enforces its own path-traversal guard).
    /// </summary>
    public static (CapabilityRegistry tools, PolicyEngine policies) CreateMinimal()
    {
        var tools = new CapabilityRegistry();
        tools.Register(new RepoFsListTool());
        tools.Register(new RepoFsReadTool());
        tools.Register(new RepoFsWriteTool());
        tools.Register(new RepoFsSearchReplaceTool());

        var policies = new PolicyEngine(new IPolicy[]
        {
            new PathAllowlist(),
            new MaxWriteSize()
        });

        return (tools, policies);
    }
}
