using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Abstractions;
using Ashlar.Orchestration.Agents;
using Ashlar.Orchestration.Architect.Models;
using Ashlar.Orchestration.GameDomain;
using Ashlar.Orchestration.GameDomain.Agents;
using Moq;
using Xunit;

namespace Ashlar.Tests.Orchestration;

/// <summary>
/// Coverage for the game domain agents and <see cref="GameDomainAgentProvider"/>.
///
/// <para>Collected here from two kernel test files so that it moves with the game layer:
/// the four execution tests came from OrchestrationDomainTemplateTests (which keeps its
/// SecurityAgent and GenericAgent cases), and the routing tests came from AgentFactoryTests.
/// Both of those stay in the kernel and can no longer name these types.</para>
/// </summary>
public class GameDomainAgentTests
{
    private static AgentSpawnSpec Spec(string domain) => new()
    {
        AgentId = $"{domain.ToLower()}-1",
        Domain = domain,
        Goal = $"Design {domain}",
    };

    private static async Task RunDomainAgent(BaseDomainAgent agent)
    {
        await agent.InitializeAsync();
        var result = await agent.ExecuteAsync();
        result.Should().NotBeNull();
    }

    // ---------------------------------------------------------------- agent execution

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

    // ---------------------------------------------------------------- factory routing

    private static ServiceProvider GameServices(bool withProvider)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IModel>());
        services.AddSingleton<AgentFactory>();
        if (withProvider)
        {
            services.AddGameDomainAgents();
        }

        return services.BuildServiceProvider();
    }

    [Theory]
    [InlineData("Combat", typeof(CombatAgent))]
    [InlineData("Economy", typeof(EconomyAgent))]
    [InlineData("Gameplay", typeof(GameplayAgent))]
    [InlineData("AI", typeof(AIAgent))]
    public void AgentFactory_routes_game_domains_through_the_provider(string domain, Type expected)
    {
        // Resolved from the container rather than constructed by hand, so this also covers
        // DI actually populating AgentFactory's optional provider parameter.
        using var provider = GameServices(withProvider: true);

        var agent = provider.GetRequiredService<AgentFactory>().CreateAgent(Spec(domain));

        agent.Should().BeOfType(expected);
        // Carried over from AgentFactoryTests.CreateAgent_CombatDomain_ReturnsCombatAgent,
        // which asserted the spawned agent takes its name from the spec's AgentId.
        agent.Name.Should().Be($"{domain.ToLower()}-1");
    }

    [Theory]
    [InlineData("Combat")]
    [InlineData("Economy")]
    [InlineData("Gameplay")]
    [InlineData("AI")]
    public void Without_the_game_layer_these_domains_fall_back_to_generic(string domain)
    {
        // The point of the extraction. A kernel with no game package installed treats these
        // as ordinary unrecognised domains rather than failing to compile without them.
        using var provider = GameServices(withProvider: false);

        provider.GetRequiredService<AgentFactory>()
            .CreateAgent(Spec(domain)).Should().BeOfType<GenericAgent>();
    }

    [Fact]
    public void AddGameDomain_registers_the_whole_game_layer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGameDomain();
        using var provider = services.BuildServiceProvider();

        // Two agent providers: the domain agents (combat/economy/ai/gameplay) and the
        // asset agents (image/audio/model3d). They are separate because the asset PORTS
        // stay in the kernel while only the game-flavoured agents move, so an application
        // can take one without the other via AddGameDomainAgents / AddGameAssetAgents.
        provider.GetServices<IDomainAgentProvider>().Select(p => p.GetType())
            .Should().BeEquivalentTo(new[] { typeof(GameDomainAgentProvider), typeof(GameAssetAgentProvider) });

        provider.GetServices<Ashlar.Orchestration.Architect.IDomainPatternProvider>()
            .Should().ContainSingle().Which.Should().BeOfType<GameDomainPatternProvider>();
    }
}
