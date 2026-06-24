using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.Configuration;
using Nexo.BackgroundAgents.DataSensitivity;
using Nexo.BackgroundAgents.Extending;
using Nexo.BackgroundAgents.Registry;
using Nexo.BackgroundAgents.Scheduling;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Architect.Models;
using Nexo.Runtime;

namespace Nexo.Tests.BackgroundAgents.SelfExtend;

/// <summary>
/// Shared helpers for SX-AUDIT invariant characterization tests.
/// </summary>
internal static class SelfExtendAuditTestSupport
{
    internal static IReadOnlyList<IPolicy> GetPolicies(PolicyEngine engine)
    {
        var field = typeof(PolicyEngine).GetField("_policies", BindingFlags.NonPublic | BindingFlags.Instance);
        return (IReadOnlyList<IPolicy>)field!.GetValue(engine)!;
    }

    internal static BackgroundAgentRegistry CreateRegistry(
        ISelfExtendRunner? selfExtendRunner = null,
        IAggressivenessModeStore? modeStore = null,
        IApprovalGate? approvalGate = null)
    {
        var scheduler = new AgentScheduler(new ScheduleExecutor(), NullLogger<AgentScheduler>.Instance);
        return new BackgroundAgentRegistry(
            scheduler,
            NullLogger<BackgroundAgentRegistry>.Instance,
            selfExtendRunner: selfExtendRunner,
            modeStore: modeStore,
            approvalGate: approvalGate);
    }

    internal static AgentSpawnSpec BuildSpec(BackgroundAgentConfig config) =>
        new BackgroundAgentSpecBuilder(new DataSensitivityRegistry(), null).BuildSpec(config);

    internal static BackgroundAgentConfig ExtenderConfig(string id, string repoRoot) => new()
    {
        Id = id,
        Role = "extender",
        Enabled = true,
        MaxDataSensitivity = "Public",
        Commands = ["extend"],
        Parameters = new Dictionary<string, object> { ["RepoRoot"] = repoRoot },
        Schedule = new BackgroundAgentSchedule { Type = ScheduleType.Interval, Interval = TimeSpan.FromMinutes(1) },
    };

    internal sealed class CountingSelfExtendRunner : ISelfExtendRunner
    {
        public int CallCount { get; private set; }

        public Task<SelfExtendRunResult> RunAsync(string repoRoot, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new SelfExtendRunResult(true, 0, 0, "counted"));
        }

        public Task<SelfExtendRunResult> RunAsync(
            string repoRoot,
            string? objective,
            string? agentName,
            string? modelProvider,
            string? modelName,
            CancellationToken cancellationToken = default) =>
            RunAsync(repoRoot, cancellationToken);
    }
}
