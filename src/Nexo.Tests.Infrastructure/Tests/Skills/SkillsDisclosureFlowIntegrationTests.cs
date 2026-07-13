using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.Abstractions.Barriers;
using Nexo.BackgroundAgents.Trust;
using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.Paths;
using Nexo.Core.Application.Skills.Models;
using Nexo.Core.Application.Skills.Ports;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Infrastructure.Skills;
using Nexo.Infrastructure.Skills.Sdk.Extensions;
using Nexo.Infrastructure.Trust;
using Nexo.Runtime.Barriers;
using Nexo.Skills.AgentFramework;
using Nexo.Skills.AgentFramework.ClassSkills;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Skills;

public sealed class SkillsDisclosureFlowIntegrationTests
{
    [Fact]
    public async Task Full_disclosure_flow_filters_tiers_and_emits_audit_trail()
    {
        var repoRoot = RepoPathResolver.FindRepoRoot();
        var services = new ServiceCollection();
        services.AddLogging();

        var packsPath = Path.Combine(repoRoot, "config", "trust-packs");
        Environment.SetEnvironmentVariable("NEXO_TRUST_POLICY_PACKS_PATH", packsPath);

        services.AddSingleton<ITrustPolicyPackRegistry>(_ => new TrustPolicyPackRegistry(packsPath));
        services.AddSingleton<IBarrierContextAccessor, StubBarrierContextAccessor>();
        services.AddSingleton<IDataDecisionAuditLog, DataDecisionAuditLog>();
        services.AddNexoSkills();
        services.AddSingleton<INexoSkillApprovalGate>(_ => new AlwaysApproveSkillGate());
        services.AddNexoAgentFrameworkSkills(options =>
        {
            options.SkillRootPaths.Add(Path.Combine(repoRoot, "skills"));
        });

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var registry = provider.GetRequiredService<ITrustPolicyPackRegistry>();
        await registry.ActivateAsync("internal-only");

        var catalog = provider.GetRequiredService<INexoSkillCatalog>();
        var auditLog = provider.GetRequiredService<IDataDecisionAuditLog>();
        var contextFactory = provider.GetRequiredService<INexoSkillExecutionContextFactory>();

        var trustedContext = contextFactory.Create(PeerTrustTier.Trusted, "integration-tester", "corr-trusted");
        var untrustedContext = contextFactory.Create(PeerTrustTier.Untrusted, "integration-tester", "corr-untrusted");

        var trustedAds = await catalog.AdvertiseAsync(trustedContext);
        var untrustedAds = await catalog.AdvertiseAsync(untrustedContext);

        trustedAds.Select(static s => s.Name).Should().Contain("nexo-trust-inspector");
        trustedAds.Select(static s => s.Name).Should().Contain("nexo-active-policy-pack");
        untrustedAds.Select(static s => s.Name).Should().Contain("nexo-trust-inspector");
        untrustedAds.Select(static s => s.Name).Should().NotContain("nexo-active-policy-pack");

        var loaded = await catalog.LoadAsync("nexo-trust-inspector", trustedContext);
        loaded.Should().Contain("Nexo Trust Inspector");

        var resource = await catalog.ReadResourceAsync(
            "nexo-trust-inspector",
            "references/pack-schema.md",
            trustedContext);
        resource.Should().Contain("skillRules");

        var scriptResult = await catalog.RunScriptAsync(
            "nexo-trust-inspector",
            "scripts/list-packs.sh",
            null,
            trustedContext);
        scriptResult.Outcome.Should().BeOneOf("success", "truncated");
        scriptResult.Output.Should().Contain("Trust policy packs");

        auditLog.GetRecent(100).Should().Contain(e => e.EventType == NexoSkillAuditEventType.SkillAdvertised);
        auditLog.GetRecent(100).Should().Contain(e => e.EventType == NexoSkillAuditEventType.SkillLoaded);
        auditLog.GetRecent(100).Should().Contain(e => e.EventType == NexoSkillAuditEventType.SkillResourceRead);
        auditLog.GetRecent(100).Should().Contain(e => e.EventType == NexoSkillAuditEventType.SkillScriptApprovalRequested);
        auditLog.GetRecent(100).Should().Contain(e => e.EventType == NexoSkillAuditEventType.SkillScriptApprovalResolved);
        auditLog.GetRecent(100).Should().Contain(e => e.EventType == NexoSkillAuditEventType.SkillScriptExecuted);
    }

    private sealed class StubBarrierContextAccessor : IBarrierContextAccessor
    {
        public BarrierContext? Current { get; private set; }

        public void Initialize(BarrierContext context) => Current = context;
    }

    private sealed class AlwaysApproveSkillGate : INexoSkillApprovalGate
    {
        public Task<NexoSkillApprovalStatus> RequestApprovalAsync(
            string actionDescription,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
            => Task.FromResult(NexoSkillApprovalStatus.Approved);
    }
}
