using FluentAssertions;
using GameDirector.Agents;
using Moq;
using Ashlar.Abstractions;
using Ashlar.Core.Domain.Agents;
using Xunit;

namespace Ashlar.Tests.GameDirector;

/// <summary>Tests for agent card.</summary>
[Trait("Category", "GameDirectorApplication")]
public sealed class AgentCardTests
{
    [Fact]
    public void BalanceWatcherAgent_exposes_expected_card_and_name()
    {
        var agent = new BalanceWatcherAgent();
        agent.Name.Should().Be("balance-watcher");

        var card = BalanceWatcherAgent.Card;
        card.Id.Should().Be("balance-watcher");
        card.Name.Should().Be("Balance Watcher");
        card.Domain.Should().Be("game.balance");
        card.Description.Should().NotBeNullOrWhiteSpace();
        card.Behaviors.Should().Contain(["watch", "analyze", "notify"]);
    }

    [Fact]
    public async Task BalanceWatcherAgent_think_returns_no_actions()
    {
        var agent = new BalanceWatcherAgent();
        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>());
        var actions = await agent.ThinkAsync(
            new AgentObservation(snapshot),
            Mock.Of<IToolbox>(),
            Mock.Of<IAgentMemory>(),
            CancellationToken.None);

        actions.Should().BeSameAs(AgentActions.None);
    }

    [Fact]
    public void MapValidatorAgent_exposes_expected_card_and_name()
    {
        var agent = new MapValidatorAgent();
        agent.Name.Should().Be("map-validator");

        var card = MapValidatorAgent.Card;
        card.Id.Should().Be("map-validator");
        card.Name.Should().Be("Map Validator");
        card.Domain.Should().Be("game.map");
        card.Behaviors.Should().Contain(["validate", "report"]);
    }

    [Fact]
    public async Task MapValidatorAgent_think_returns_no_actions()
    {
        var agent = new MapValidatorAgent();
        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>());
        var actions = await agent.ThinkAsync(
            new AgentObservation(snapshot),
            Mock.Of<IToolbox>(),
            Mock.Of<IAgentMemory>(),
            CancellationToken.None);

        actions.Should().BeSameAs(AgentActions.None);
    }
}
