using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Architect.Models;
using Xunit;

namespace Nexo.Tests.Orchestration;

public class OrchestrationDomainTemplateTests
{
    private static AgentSpawnSpec Spec(string domain) => new()
    {
        AgentId = $"{domain.ToLower()}-1",
        Domain = domain,
        Goal = $"Design {domain}",
    };

    [Fact]
    public async Task GameplayAgent_executes_with_mock_output() =>
        await RunDomainAgent(new GameplayAgent(Spec("Gameplay"), NullLogger<GameplayAgent>.Instance));

    [Fact]
    public async Task CombatAgent_executes_with_mock_output() =>
        await RunDomainAgent(new CombatAgent(Spec("Combat"), NullLogger<CombatAgent>.Instance));

    [Fact]
    public async Task EconomyAgent_executes_with_mock_output() =>
        await RunDomainAgent(new EconomyAgent(Spec("Economy"), NullLogger<EconomyAgent>.Instance));

    [Fact]
    public async Task AIAgent_executes_with_mock_output() =>
        await RunDomainAgent(new AIAgent(Spec("AI"), NullLogger<AIAgent>.Instance));

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
