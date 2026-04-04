using Nexo.Abstractions;
using Nexo.Policies.Dev;
using Nexo.Runtime;
using Nexo.Tools.Dev;

namespace Nexo.BackgroundAgents.HostRunners;

/// <summary>
/// Shared factory for minimal repo-fs toolbox (write, search_replace) and policy (path allowlist, max write size).
/// Used by SelfExtendRunnerAdapter to avoid duplicating tool/policy setup.
/// </summary>
internal static class RepoFsToolboxFactory
{
    /// <summary>
    /// Creates a minimal toolbox with RepoFsWriteTool and RepoFsSearchReplaceTool,
    /// and a policy engine with PathAllowlist and MaxWriteSize.
    /// </summary>
    public static (CapabilityRegistry tools, PolicyEngine policies) CreateMinimal()
    {
        var tools = new CapabilityRegistry();
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
