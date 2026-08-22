using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Orchestration.Agents;
using Ashlar.Orchestration.Agents.Templates;
using Ashlar.Orchestration.Architect.Models;
using Xunit;

namespace Ashlar.Tests.Orchestration;

/// <summary>Tests for orchestration domain template.</summary>
public class OrchestrationDomainTemplateTests
{
    /// <summary>Spec.</summary>
    /// <param name="domain">Domain.</param>
    private static AgentSpawnSpec Spec(string domain) => new()
    {
        AgentId = $"{domain.ToLower()}-1",
        Domain = domain,
        Goal = $"Design {domain}",
    };

    /// <summary>Gameplay agent_executes_with_mock_output.</summary>
    [Fact]
    public async Task GameplayAgent_executes_with_mock_output() =>
        await RunDomainAgent(new GameplayAgent(Spec("Gameplay"), NullLogger<GameplayAgent>.Instance));

    /// <summary>Combat agent_executes_with_mock_output.</summary>
    [Fact]
    public async Task CombatAgent_executes_with_mock_output() =>
        await RunDomainAgent(new CombatAgent(Spec("Combat"), NullLogger<CombatAgent>.Instance));

    /// <summary>Economy agent_executes_with_mock_output.</summary>
    [Fact]
    public async Task EconomyAgent_executes_with_mock_output() =>
        await RunDomainAgent(new EconomyAgent(Spec("Economy"), NullLogger<EconomyAgent>.Instance));

    /// <summary>Ai agent_executes_with_mock_output.</summary>
    [Fact]
    public async Task AIAgent_executes_with_mock_output() =>
        await RunDomainAgent(new AIAgent(Spec("AI"), NullLogger<AIAgent>.Instance));

    /// <summary>Security agent_executes_with_mock_output.</summary>
    [Fact]
    public async Task SecurityAgent_executes_with_mock_output() =>
        await RunDomainAgent(new SecurityAgent(Spec("Security"), NullLogger<SecurityAgent>.Instance));

    [Fact]
    public async Task GenericAgent_returns_placeholder_result()
    {
        var agent = new GenericAgent(Spec("Unknown"), NullLogger<GenericAgent>.Instance);
        await agent.InitializeAsync();
        var result = await agent.ExecuteAsync();
        result.Should().NotBeNull();
    }

    private static async Task RunDomainAgent(BaseDomainAgent agent)
    {
        await agent.InitializeAsync();
        var result = await agent.ExecuteAsync();
        result.Should().NotBeNull();
    }
}
