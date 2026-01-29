using System.Text.Json;
using FluentAssertions;
using Moq;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.Registry;
using Nexo.BackgroundAgents.Tools;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Tools;

public class EnableAgentToolTests
{
    [Fact]
    public void Id_IsEnableAgent()
    {
        var tool = new EnableAgentTool(Mock.Of<IBackgroundAgentRegistry>());
        tool.Id.Should().Be("enable_agent");
    }

    [Fact]
    public async Task InvokeAsync_MissingAgentId_ReturnsError()
    {
        var tool = new EnableAgentTool(Mock.Of<IBackgroundAgentRegistry>());
        var args = JsonSerializer.SerializeToElement(new { });
        var call = new ToolCall("enable_agent", args);
        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>());

        var result = await tool.InvokeAsync(call, snapshot, default);

        result.Delta.Log.Should().Contain(l => l.Contains("agentId is required"));
    }

    [Fact]
    public async Task InvokeAsync_CallsRegistryStartAsync()
    {
        var registry = new Mock<IBackgroundAgentRegistry>();
        registry.Setup(r => r.StartAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var tool = new EnableAgentTool(registry.Object);
        var args = JsonSerializer.SerializeToElement(new { agentId = "test-agent" });
        var call = new ToolCall("enable_agent", args);
        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>());

        var result = await tool.InvokeAsync(call, snapshot, default);

        result.Delta.Log.Should().Contain(l => l.Contains("Started"));
        registry.Verify(r => r.StartAsync("test-agent", It.IsAny<CancellationToken>()), Times.Once);
    }
}
