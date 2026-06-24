using Nexo.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.BackgroundAgents.Configuration;
using Nexo.BackgroundAgents.Registry;
using Nexo.Orchestration.Agents;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.SelfExtend;

/// <summary>
/// SX-AUDIT invariant B — monotonic policy narrowing for self-created agents.
/// See <c>docs/SELF-EXTEND-AUDIT.md#invariant-b-monotonic-policy-narrowing</c>.
/// </summary>
public sealed class SelfExtendInvariantBPolicyNarrowingTests
{
    [Fact]
    public async Task Characterization_register_async_accepts_child_with_broader_exfiltration_than_parent()
    {
        var registry = SelfExtendAuditTestSupport.CreateRegistry();
        var parent = RestrictiveConfig("creator-parent", parentId: null);
        var child = BroadenedChildConfig("creator-child", parentId: "creator-parent");

        await registry.RegisterAsync(
            new GenericAgent(SelfExtendAuditTestSupport.BuildSpec(parent), NullLogger<GenericAgent>.Instance),
            parent);
        await registry.RegisterAsync(
            new GenericAgent(SelfExtendAuditTestSupport.BuildSpec(child), NullLogger<GenericAgent>.Instance),
            child);

        var stored = registry.GetAgent("creator-child");
        stored.Should().NotBeNull();
        stored!.Config.ExfiltrationPolicy.BlockExternalLLMs.Should().BeFalse();
        stored.Config.ExfiltrationPolicy.RequireLocalOnly.Should().BeFalse();
        stored.Config.ExfiltrationPolicy.MaxAllowedLevel.Should().Be("Secret");
    }

    [Fact]
    public void Characterization_self_extend_snapshot_omits_agentId_so_data_exfiltration_policy_fail_open()
    {
        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>
        {
            ["RepoRoot"] = "/workspace",
            ["AgentName"] = "runtime-planner"
        });

        snapshot.Data.Should().ContainKey("AgentName");
        snapshot.Data.Should().NotContainKey("agentId",
            "SelfExtendRunnerAdapter.BuildSnapshot sets AgentName only — DataExfiltrationPolicy.Approve fail-opens without agentId");
    }

    /// <summary>
    /// Rejection test for invariant B. Skipped because spawn/registration does not compare child vs parent envelope.
    /// </summary>
    [Fact(Skip = "GAP: RegisterAsync stores broader ExfiltrationPolicy with no subset check vs creator — see docs/SELF-EXTEND-AUDIT.md#invariant-b-monotonic-policy-narrowing")]
    public async Task Rejection_spawn_spec_with_broader_envelope_than_creator_is_refused()
    {
        var registry = SelfExtendAuditTestSupport.CreateRegistry();
        var parent = RestrictiveConfig("creator-parent", parentId: null);
        var child = BroadenedChildConfig("creator-child", parentId: "creator-parent");

        await registry.RegisterAsync(
            new GenericAgent(SelfExtendAuditTestSupport.BuildSpec(parent), NullLogger<GenericAgent>.Instance),
            parent);

        var act = () => registry.RegisterAsync(
            new GenericAgent(SelfExtendAuditTestSupport.BuildSpec(child), NullLogger<GenericAgent>.Instance),
            child);

        await act.Should().ThrowAsync<InvalidOperationException>(
            "invariant B requires broader MaxAllowedLevel or relaxed BlockExternalLLMs/RequireLocalOnly to be REFUSED");
    }

    private static BackgroundAgentConfig RestrictiveConfig(string id, string? parentId) => new()
    {
        Id = id,
        ParentId = parentId,
        Role = "extender",
        Enabled = true,
        MaxDataSensitivity = "Public",
        Commands = ["extend"],
        ExfiltrationPolicy = new ExfiltrationPolicy
        {
            MaxAllowedLevel = "Public",
            BlockExternalLLMs = true,
            BlockWebSearch = true,
            BlockNetworkExports = true,
            RequireLocalOnly = true
        }
    };

    private static BackgroundAgentConfig BroadenedChildConfig(string id, string parentId) => new()
    {
        Id = id,
        ParentId = parentId,
        Role = "extender",
        Enabled = true,
        MaxDataSensitivity = "Secret",
        Commands = ["extend"],
        ExfiltrationPolicy = new ExfiltrationPolicy
        {
            MaxAllowedLevel = "Secret",
            BlockExternalLLMs = false,
            BlockWebSearch = false,
            BlockNetworkExports = false,
            RequireLocalOnly = false
        }
    };
}
