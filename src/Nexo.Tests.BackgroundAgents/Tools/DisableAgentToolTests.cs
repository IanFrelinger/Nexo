using System.Text.Json;
using FluentAssertions;
using Moq;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.Registry;
using Nexo.BackgroundAgents.Tools;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Tools;

public class DisableAgentToolTests
{
    [Fact]
    public void Id_IsDisableAgent()
    {
        var tool = new DisableAgentTool(Mock.Of<IBackgroundAgentRegistry>());
        tool.Id.Should().Be("disable_agent");
    }

    [Fact]
    public async Task InvokeAsync_CallsRegistryStopAsync()
    {
        var registry = new Mock<IBackgroundAgentRegistry>();
        registry.Setup(r => r.StopAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var tool = new DisableAgentTool(registry.Object);
        var args = JsonSerializer.SerializeToElement(new { agentId = "test-agent" });
        var call = new ToolCall("disable_agent", args);
        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>());

        var result = await tool.InvokeAsync(call, snapshot, default);

        result.Delta.Log.Should().Contain(l => l.Contains("Stopped"));
        registry.Verify(r => r.StopAsync("test-agent", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_MissingAgentId_ReturnsError()
    {
        var tool = new DisableAgentTool(Mock.Of<IBackgroundAgentRegistry>());
        var args = JsonSerializer.SerializeToElement(new { });
        var call = new ToolCall("disable_agent", args);
        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>());

        var result = await tool.InvokeAsync(call, snapshot, default);

        result.Delta.Log.Should().Contain(l => l.Contains("agentId is required"));
    }

    [Fact]
    public async Task InvokeAsync_RegistryFailure_ReturnsErrorPayload()
    {
        var registry = new Mock<IBackgroundAgentRegistry>();
        registry.Setup(r => r.StopAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("stop failed"));
        var tool = new DisableAgentTool(registry.Object);
        var args = JsonSerializer.SerializeToElement(new { agentId = "bad-agent" });
        var call = new ToolCall("disable_agent", args);
        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>());

        var result = await tool.InvokeAsync(call, snapshot, default);

        result.Delta.Log.Should().Contain(l => l.Contains("stop failed"));
    }

    [Fact]
    public async Task InvokeAsync_InvalidJsonArgs_TreatedAsMissingAgentId()
    {
        var tool = new DisableAgentTool(Mock.Of<IBackgroundAgentRegistry>());
        var call = new ToolCall("disable_agent", JsonSerializer.SerializeToElement("{not-json"));
        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>());

        var result = await tool.InvokeAsync(call, snapshot, default);

        result.Delta.Log.Should().Contain(l => l.Contains("agentId is required"));
    }
}
