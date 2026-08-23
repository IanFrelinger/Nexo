using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.BackgroundAgents.Configuration;
using Ashlar.BackgroundAgents.Registry;
using Ashlar.Orchestration.Agents;
using Ashlar.Tests.BackgroundAgents.Registry;
using Xunit;

namespace Ashlar.Tests.BackgroundAgents.SelfExtend;

/// <summary>
/// SX-HARDEN finding 1 — machine-origin registration must carry a resolvable parent.
/// </summary>
public sealed class SelfExtendHardeningFinding1Tests
{
    [Fact]
    public async Task Rejection_machine_origin_with_blank_parent_is_refused()
    {
        var registry = SelfExtendAuditTestSupport.CreateRegistry();
        var config = RootConfig("orphan-machine-agent");

        var act = () => registry.RegisterAsync(
            new GenericAgent(SelfExtendAuditTestSupport.BuildSpec(config), NullLogger<GenericAgent>.Instance),
            config);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ParentId is required*");
    }

    [Fact]
    public async Task Rejection_machine_origin_with_unregistered_parent_is_refused()
    {
        var registry = SelfExtendAuditTestSupport.CreateRegistry();
        var config = new BackgroundAgentConfig
        {
            Id = "missing-parent-child",
            ParentId = "not-registered-parent",
            Role = "extender",
            Enabled = true,
            MaxDataSensitivity = "Public",
            Commands = ["extend"],
        };

        var act = () => registry.RegisterAsync(
            new GenericAgent(SelfExtendAuditTestSupport.BuildSpec(config), NullLogger<GenericAgent>.Instance),
            config);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not registered*");
    }

    [Fact]
    public async Task Admission_authored_origin_with_blank_parent_registers()
    {
        var registry = SelfExtendAuditTestSupport.CreateRegistry();
        var config = RootConfig("balance-watcher");

        await registry.RegisterAuthoredAsync(
            new GenericAgent(SelfExtendAuditTestSupport.BuildSpec(config), NullLogger<GenericAgent>.Instance),
            config);

        registry.GetAgent("balance-watcher").Should().NotBeNull();
    }

    private static BackgroundAgentConfig RootConfig(string id) => new()
    {
        Id = id,
        Role = "monitor",
        Enabled = true,
        MaxDataSensitivity = "Public",
        Commands = ["ping"],
    };
}
