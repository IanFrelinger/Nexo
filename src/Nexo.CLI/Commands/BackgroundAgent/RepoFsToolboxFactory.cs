using Nexo.Abstractions;
using Nexo.Policies.Dev;
using Nexo.Runtime;
using Nexo.Tools.Dev;

namespace Nexo.CLI.Commands.BackgroundAgent;

/// <summary>
/// Shared factory for minimal repo-fs toolbox (write, search_replace) and policy (path allowlist, max write size).
/// Used by SelfExtendRunnerAdapter and SelfExtendToolRuntime (Demo) to avoid duplicating tool/policy setup.
/// </summary>
internal static class RepoFsToolboxFactory
{
    /// <summary>
    /// Creates a minimal toolbox with RepoFsWriteTool and RepoFsSearchReplaceTool,
    /// and a policy engine with PathAllowlist and MaxWriteSize.
    /// Returns concrete types so callers (e.g. Demo) can add more tools to the same registry.
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
