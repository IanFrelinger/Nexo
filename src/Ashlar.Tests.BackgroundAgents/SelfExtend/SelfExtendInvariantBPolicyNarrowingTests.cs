using Ashlar.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.BackgroundAgents.Configuration;
using Ashlar.BackgroundAgents.Registry;
using Ashlar.Orchestration.Agents;
using Ashlar.Tests.BackgroundAgents.Registry;
using Xunit;

namespace Ashlar.Tests.BackgroundAgents.SelfExtend;

/// <summary>
/// SX-ENFORCE invariant B — monotonic policy narrowing for machine-spawned agents.
/// </summary>
public sealed class SelfExtendInvariantBPolicyNarrowingTests
{
    [Fact]
    public async Task Admission_equal_or_stricter_child_envelope_registers_successfully()
    {
        var registry = SelfExtendAuditTestSupport.CreateRegistry();
        var parent = RestrictiveConfig("creator-parent", parentId: null);
        var child = new BackgroundAgentConfig
        {
            Id = "creator-child",
            ParentId = "creator-parent",
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

        await registry.RegisterAuthoredAsync(
            new GenericAgent(SelfExtendAuditTestSupport.BuildSpec(parent), NullLogger<GenericAgent>.Instance),
            parent);
        await registry.RegisterAsync(
            new GenericAgent(SelfExtendAuditTestSupport.BuildSpec(child), NullLogger<GenericAgent>.Instance),
            child);

        registry.GetAgent("creator-child").Should().NotBeNull();
    }

    [Fact]
    public void Characterization_self_extend_snapshot_includes_agentId_for_exfiltration_policy()
    {
        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>
        {
            ["RepoRoot"] = "/workspace",
            ["AgentName"] = "runtime-planner",
            ["agentId"] = "runtime-planner",
            ["selfExtendAdmission"] = true
        });

        snapshot.Data.Should().ContainKey("agentId");
        snapshot.Data["agentId"].Should().Be("runtime-planner");
    }

    [Fact]
    public async Task Rejection_spawn_spec_with_broader_envelope_than_creator_is_refused()
    {
        var registry = SelfExtendAuditTestSupport.CreateRegistry();
        var parent = RestrictiveConfig("creator-parent", parentId: null);
        var child = BroadenedChildConfig("creator-child", parentId: "creator-parent");

        await registry.RegisterAuthoredAsync(
            new GenericAgent(SelfExtendAuditTestSupport.BuildSpec(parent), NullLogger<GenericAgent>.Instance),
            parent);

        var act = () => registry.RegisterAsync(
            new GenericAgent(SelfExtendAuditTestSupport.BuildSpec(child), NullLogger<GenericAgent>.Instance),
            child);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*BlockExternalLLMs*");
    }

    [Fact]
    public async Task Admission_human_authored_root_without_parent_registers_without_narrowing_check()
    {
        var registry = SelfExtendAuditTestSupport.CreateRegistry();
        var root = BroadenedChildConfig("balance-watcher", parentId: null);

        await registry.RegisterAuthoredAsync(
            new GenericAgent(SelfExtendAuditTestSupport.BuildSpec(root), NullLogger<GenericAgent>.Instance),
            root);

        registry.GetAgent("balance-watcher")!.Config.ExfiltrationPolicy.BlockExternalLLMs.Should().BeFalse();
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

    private static BackgroundAgentConfig BroadenedChildConfig(string id, string? parentId) => new()
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
