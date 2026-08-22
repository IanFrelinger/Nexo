using System.Text.Json;
using FluentAssertions;
using Moq;
using Ashlar.Abstractions;
using Ashlar.BackgroundAgents.Registry;
using Ashlar.BackgroundAgents.Tools;
using Xunit;

namespace Ashlar.Tests.BackgroundAgents.Tools;

/// <summary>Tests for restart agent tool.</summary>
public class RestartAgentToolTests
{
    [Fact]
    public void Id_IsRestartAgent()
    {
        var tool = new RestartAgentTool(Mock.Of<IBackgroundAgentRegistry>());
        tool.Id.Should().Be("restart_agent");
    }

    [Fact]
    public async Task InvokeAsync_CallsStopThenStart()
    {
        var registry = new Mock<IBackgroundAgentRegistry>();
        registry.Setup(r => r.StopAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        registry.Setup(r => r.StartAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var tool = new RestartAgentTool(registry.Object);
        var args = JsonSerializer.SerializeToElement(new { agentId = "test-agent" });
        var call = new ToolCall("restart_agent", args);
        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>());
        var result = await tool.InvokeAsync(call, snapshot, default);
        result.Delta.Log.Should().Contain(l => l.Contains("Restarted"));
        registry.Verify(r => r.StopAsync("test-agent", It.IsAny<CancellationToken>()), Times.Once);
        registry.Verify(r => r.StartAsync("test-agent", It.IsAny<CancellationToken>()), Times.Once);
    }
}
