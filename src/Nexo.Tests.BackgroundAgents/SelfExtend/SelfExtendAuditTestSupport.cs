using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.Configuration;
using Nexo.BackgroundAgents.DataSensitivity;
using Nexo.BackgroundAgents.Extending;
using Nexo.BackgroundAgents.Registry;
using Nexo.BackgroundAgents.Scheduling;
using Nexo.Certification.Contracts;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;
using Nexo.Infrastructure.Certification;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Architect.Models;
using Nexo.Runtime;

namespace Nexo.Tests.BackgroundAgents.SelfExtend;

/// <summary>
/// Shared helpers for SX-AUDIT / SX-ENFORCE invariant characterization tests.
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
        IApprovalGate? approvalGate = null,
        IDataSensitivityRegistry? sensitivityRegistry = null)
    {
        var scheduler = new AgentScheduler(new ScheduleExecutor(), NullLogger<AgentScheduler>.Instance);
        return new BackgroundAgentRegistry(
            scheduler,
            NullLogger<BackgroundAgentRegistry>.Instance,
            selfExtendRunner: selfExtendRunner,
            modeStore: modeStore,
            approvalGate: approvalGate,
            sensitivityRegistry: sensitivityRegistry ?? new DataSensitivityRegistry());
    }

    internal static InMemoryAggressivenessModeStore ActiveModeStore()
    {
        var store = new InMemoryAggressivenessModeStore();
        store.SetMode(BackgroundAgentAggressivenessMode.Active);
        return store;
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

    internal static (ICertificationRecordStore store, string brickId, string source) SeedCertifiedBrickAdmission(
        string className,
        string? namespaceName = "Nexo.Bricks.AuditGap")
    {
        var source = $"namespace {namespaceName}; public sealed class {className} {{ }}";
        var brickId = Nexo.BackgroundAgents.Security.BrickAdmissionPathHelper.ClassNameToBrickId(className);
        var hash = BrickContentHasher.ComputeSha256(source);
        var data = new CertificationRecordData
        {
            Status = "PASS",
            Stage = "admit",
            Admitted = true,
            Signed = true,
            Timestamp = DateTimeOffset.UtcNow,
            BrickId = brickId,
            ContentHash = hash
        };
        data = data with { Signature = CertificationRecordSigning.Sign(data) };

        var store = new InMemoryCertificationRecordStore();
        store.Save(new CertificationRecord
        {
            Status = data.Status,
            Stage = data.Stage,
            Admitted = data.Admitted,
            Signed = data.Signed,
            Timestamp = data.Timestamp,
            BrickId = data.BrickId,
            ContentHash = data.ContentHash,
            Signature = data.Signature
        });

        return (store, brickId, source);
    }

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

        public Task<SelfExtendRunResult> RunAsync(
            string repoRoot,
            string? objective,
            string? agentName,
            string? modelProvider,
            string? modelName,
            string? agentId,
            CancellationToken cancellationToken = default) =>
            RunAsync(repoRoot, cancellationToken);
    }
}
