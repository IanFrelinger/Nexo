using FluentAssertions;
using Moq;
using Ashlar.Abstractions;
using Ashlar.BackgroundAgents.DataSensitivity;
using Ashlar.BackgroundAgents.Registry;
using Ashlar.BackgroundAgents.Security;
using Ashlar.Policies;
using Ashlar.Runtime;
using Xunit;

namespace Ashlar.Tests.BackgroundAgents.Security;

/// <summary>Tests for background agent policy engine factory gap coverage.</summary>
public sealed class BackgroundAgentPolicyEngineFactoryGapCoverageTests
{
    [Fact]
    public void Create_merges_additional_policies_after_exfiltration_policy()
    {
        var registry = new Mock<IBackgroundAgentRegistry>();
        var sensitivity = new DataSensitivityRegistry();
        var allowAll = new AllowAllPolicy();

        var engine = BackgroundAgentPolicyEngineFactory.Create(
            registry.Object,
            sensitivity,
            new IPolicy[] { allowAll });

        engine.Should().NotBeNull();
        var call = new ToolCall("any_tool", System.Text.Json.JsonSerializer.SerializeToElement(new { }));
        engine.Approve(call, new WorldSnapshot(0, new Dictionary<string, object?>()), out var reason).Should().BeTrue();
        reason.Should().Be("OK");
    }
}
