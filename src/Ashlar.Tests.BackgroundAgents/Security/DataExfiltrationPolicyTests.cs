using System.Text.Json;
using FluentAssertions;
using Moq;
using Ashlar.Abstractions;
using Ashlar.BackgroundAgents.Configuration;
using Ashlar.BackgroundAgents.DataSensitivity;
using Ashlar.BackgroundAgents.Registry;
using Ashlar.BackgroundAgents.Security;
using Xunit;

namespace Ashlar.Tests.BackgroundAgents.Security;

/// <summary>Tests for data exfiltration policy.</summary>
public class DataExfiltrationPolicyTests
{
    private static ToolCall ToolCall(string id, object? args = null)
    {
        var json = args != null ? JsonSerializer.SerializeToElement(args) : JsonSerializer.SerializeToElement(new { });
        /// <summary>Tool call.</summary>
        return new ToolCall(id, json);
    }

    private static WorldSnapshot Snapshot(int tick = 0, IReadOnlyDictionary<string, object?>? data = null)
    {
        return new WorldSnapshot(tick, data ?? new Dictionary<string, object?>());
    }

    [Fact]
    public void Approve_WhenNoAgentIdInSnapshot_Allows()
    {
        var registry = new Mock<IBackgroundAgentRegistry>();
        var sensitivityRegistry = new DataSensitivityRegistry();
        var policy = new DataExfiltrationPolicy(registry.Object, sensitivityRegistry);

        var call = ToolCall("web_search");
        var snapshot = Snapshot(0, new Dictionary<string, object?>());

        policy.Approve(call, snapshot, out var reason).Should().BeTrue();
        reason.Should().Be("OK");
    }

    [Fact]
    public void Approve_WhenAgentIdNotInRegistry_Allows()
    {
        var registry = new Mock<IBackgroundAgentRegistry>();
        registry.Setup(r => r.GetAgent("agent-1")).Returns((BackgroundAgentInstance?)null);
        var sensitivityRegistry = new DataSensitivityRegistry();
        var policy = new DataExfiltrationPolicy(registry.Object, sensitivityRegistry);

        var call = ToolCall("web_search");
        var snapshot = Snapshot(0, new Dictionary<string, object?> { ["agentId"] = "agent-1" });

        policy.Approve(call, snapshot, out var reason).Should().BeTrue();
        reason.Should().Be("OK");
    }

    [Fact]
    public void Approve_WhenPolicyBlocksWebSearch_Denies()
    {
        var instance = new BackgroundAgentInstance
        {
            Config = new BackgroundAgentConfig
            {
                Id = "agent-1",
                ExfiltrationPolicy = new ExfiltrationPolicy { BlockWebSearch = true }
            }
        };
        var registry = new Mock<IBackgroundAgentRegistry>();
        registry.Setup(r => r.GetAgent("agent-1")).Returns(instance);
        var sensitivityRegistry = new DataSensitivityRegistry();
        var policy = new DataExfiltrationPolicy(registry.Object, sensitivityRegistry);

        var call = ToolCall("web_search");
        var snapshot = Snapshot(0, new Dictionary<string, object?> { ["agentId"] = "agent-1" });

        policy.Approve(call, snapshot, out var reason).Should().BeFalse();
        reason.Should().Contain("web search");
    }

    [Fact]
    public void Approve_WhenPolicyBlocksExternalLLM_Denies()
    {
        var instance = new BackgroundAgentInstance
        {
            Config = new BackgroundAgentConfig
            {
                Id = "agent-1",
                ExfiltrationPolicy = new ExfiltrationPolicy { BlockExternalLLMs = true }
            }
        };
        var registry = new Mock<IBackgroundAgentRegistry>();
        registry.Setup(r => r.GetAgent("agent-1")).Returns(instance);
        var sensitivityRegistry = new DataSensitivityRegistry();
        var policy = new DataExfiltrationPolicy(registry.Object, sensitivityRegistry);

        var call = ToolCall("complete");
        var snapshot = Snapshot(0, new Dictionary<string, object?> { ["agentId"] = "agent-1" });

        policy.Approve(call, snapshot, out var reason).Should().BeFalse();
        reason.Should().Contain("external LLM");
    }

    [Fact]
    public void Approve_WhenPolicyBlocksNetworkExports_Denies()
    {
        var instance = new BackgroundAgentInstance
        {
            Config = new BackgroundAgentConfig
            {
                Id = "agent-1",
                ExfiltrationPolicy = new ExfiltrationPolicy { BlockNetworkExports = true }
            }
        };
        var registry = new Mock<IBackgroundAgentRegistry>();
        registry.Setup(r => r.GetAgent("agent-1")).Returns(instance);
        var sensitivityRegistry = new DataSensitivityRegistry();
        var policy = new DataExfiltrationPolicy(registry.Object, sensitivityRegistry);

        var call = ToolCall("export");
        var snapshot = Snapshot(0, new Dictionary<string, object?> { ["agentId"] = "agent-1" });

        policy.Approve(call, snapshot, out var reason).Should().BeFalse();
        reason.Should().Contain("network export");
    }

    [Fact]
    public void Approve_WhenPolicyAllows_Allows()
    {
        var instance = new BackgroundAgentInstance
        {
            Config = new BackgroundAgentConfig
            {
                Id = "agent-1",
                ExfiltrationPolicy = new ExfiltrationPolicy
                {
                    BlockWebSearch = false,
                    BlockExternalLLMs = false,
                    BlockNetworkExports = false
                }
            }
        };
        var registry = new Mock<IBackgroundAgentRegistry>();
        registry.Setup(r => r.GetAgent("agent-1")).Returns(instance);
        var sensitivityRegistry = new DataSensitivityRegistry();
        var policy = new DataExfiltrationPolicy(registry.Object, sensitivityRegistry);

        var call = ToolCall("web_search");
        var snapshot = Snapshot(0, new Dictionary<string, object?> { ["agentId"] = "agent-1" });

        policy.Approve(call, snapshot, out var reason).Should().BeTrue();
        reason.Should().Be("OK");
    }

    [Fact]
    public void Approve_WhenRequireLocalOnlyAndToolIsWebSearch_Denies()
    {
        var instance = new BackgroundAgentInstance
        {
            Config = new BackgroundAgentConfig
            {
                Id = "agent-1",
                ExfiltrationPolicy = new ExfiltrationPolicy { RequireLocalOnly = true }
            }
        };
        var registry = new Mock<IBackgroundAgentRegistry>();
        registry.Setup(r => r.GetAgent("agent-1")).Returns(instance);
        var sensitivityRegistry = new DataSensitivityRegistry();
        var policy = new DataExfiltrationPolicy(registry.Object, sensitivityRegistry);

        var call = ToolCall("web_search");
        var snapshot = Snapshot(0, new Dictionary<string, object?> { ["agentId"] = "agent-1" });

        policy.Approve(call, snapshot, out var reason).Should().BeFalse();
        reason.Should().Contain("local-only");
    }
}
