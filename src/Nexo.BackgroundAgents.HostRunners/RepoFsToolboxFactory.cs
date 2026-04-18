using Nexo.Abstractions;
using Nexo.BackgroundAgents.Observations;
using Nexo.Policies.Dev;
using Nexo.Runtime;
using Nexo.Tools.Dev;

namespace Nexo.BackgroundAgents.HostRunners;

/// <summary>
/// Shared factory for the repo development toolbox. The "minimal" variant gives the
/// planner the four file-system primitives (read, list, write, search_replace);
/// the "withBuildTest" variant additionally exposes <c>dotnet.build</c> and
/// <c>dotnet.test</c> behind a <see cref="BuildTestBudget"/> rate-limiter so the
/// planner can probe the codebase's health between writes without burning the
/// per-cycle deadline on a build/test loop.
///
/// <para>The factory is the single place that decides which tools the planner
/// sees, so any new capability can be added here without touching agent or
/// runner code. When an <see cref="IObservationStore"/> is supplied, build/test
/// invocations are decorated to publish their outcomes into the observations
/// log — closing the planner ↔ tester feedback loop for tools the planner
/// triggered itself.</para>
/// </summary>
internal static class RepoFsToolboxFactory
{
    /// <summary>
    /// Creates the original minimal toolbox (read/list/write/search_replace).
    /// Preserved for callers that don't want build/test capability surface.
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

    /// <summary>
    /// Creates a toolbox with the minimal repo-fs tools plus <c>dotnet.build</c>
    /// and <c>dotnet.test</c>, gated by a <see cref="BuildTestBudget"/>. When
    /// <paramref name="observations"/> is supplied, build/test invocations also
    /// publish a <see cref="RuntimeObservation"/> per call so the next planner
    /// cycle sees the outcome of the previous probe.
    /// </summary>
    /// <param name="observations">Optional sink for build/test observations.</param>
    /// <param name="source">Logical source label for emitted observations (typically the agent id).</param>
    /// <returns>The toolbox, the policy engine, and the budget instance — exposed so
    /// callers (typically the agent runner) can call <see cref="BuildTestBudget.Reset"/>
    /// at cycle boundaries.</returns>
    public static (CapabilityRegistry tools, PolicyEngine policies, BuildTestBudget budget) CreateWithBuildTest(
        IObservationStore? observations = null,
        string source = "self-extend",
        string? objectiveId = null)
    {
        var tools = new CapabilityRegistry();
        tools.Register(new RepoFsListTool());
        tools.Register(new RepoFsReadTool());
        tools.Register(new RepoFsWriteTool());
        tools.Register(new RepoFsSearchReplaceTool());

        ITool buildTool = new DotnetBuildTool();
        ITool testTool = new DotnetTestTool();
        if (observations is not null)
        {
            buildTool = new ObservingTool(buildTool, observations,
                ObservingTool.DotnetProjector(source, ObservationKind.Build, objectiveId));
            testTool = new ObservingTool(testTool, observations,
                ObservingTool.DotnetProjector(source, ObservationKind.Test, objectiveId));
        }
        tools.Register(buildTool);
        tools.Register(testTool);

        var budget = new BuildTestBudget();
        var policies = new PolicyEngine(new IPolicy[]
        {
            new PathAllowlist(),
            new MaxWriteSize(),
            budget
        });

        return (tools, policies, budget);
    }
}
