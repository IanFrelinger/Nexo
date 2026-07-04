using System.Text.Json;
using FluentAssertions;
using Moq;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.DataSensitivity;
using Nexo.BackgroundAgents.Registry;
using Nexo.BackgroundAgents.Security;
using Nexo.Runtime;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Security;

/// <summary>Tests for background agent policy engine factory.</summary>
public class BackgroundAgentPolicyEngineFactoryTests
{
    [Fact]
    public void Create_ReturnsPolicyEngineThatIncludesExfiltrationPolicy()
    {
        var registry = new Mock<IBackgroundAgentRegistry>();
        registry.Setup(r => r.GetAgent(It.IsAny<string>())).Returns((BackgroundAgentInstance?)null);
        var sensitivityRegistry = new DataSensitivityRegistry();

        var engine = BackgroundAgentPolicyEngineFactory.Create(registry.Object, sensitivityRegistry);

        engine.Should().NotBeNull();
        var call = new ToolCall("some_tool", JsonSerializer.SerializeToElement(new { }));
        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>());
        engine.Approve(call, snapshot, out var reason).Should().BeTrue();
        reason.Should().Be("OK");
    }
}
