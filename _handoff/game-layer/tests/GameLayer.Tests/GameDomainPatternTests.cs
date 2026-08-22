using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Orchestration.Architect;
using Ashlar.Orchestration.GameDomain;
using Xunit;

namespace Ashlar.Tests.Orchestration;

/// <summary>
/// Coverage for <see cref="GameDomainPatternProvider"/> — the game vocabulary lifted out of
/// DomainRecognizer's hardcoded table.
///
/// <para>This file names the game types, so it moves with the game layer (see
/// scripts/handoff/extract-game-layer.sh). Kernel-side coverage of the seam itself lives in
/// DomainRecognizerTests, which stays.</para>
/// </summary>
public class GameDomainPatternTests
{
    private static DomainRecognizer WithGameLayer() =>
        new(NullLogger<DomainRecognizer>.Instance, new[] { new GameDomainPatternProvider() });

    [Theory]
    [InlineData("rebalance the combat damage and armor values", "Combat")]
    [InlineData("reload the rifle ammo after melee", "Combat")]
    [InlineData("players buy and sell items at the vendor", "Economy")]
    [InlineData("drop gold coin loot as a reward", "Economy")]
    [InlineData("add a quest with a level progress objective", "Gameplay")]
    [InlineData("pvp matchmaking lobby for the session", "Gameplay")]
    public void Game_vocabulary_is_recognised_once_the_provider_is_registered(string request, string expected)
    {
        WithGameLayer().RecognizeDomains(request).Should().Contain(expected);
    }

    [Theory]
    [InlineData("improve the npc pathfinding and steering")]
    [InlineData("the non-player character needs better navigation")]
    public void Game_AI_terms_are_recognised_as_AI(string request)
    {
        WithGameLayer().RecognizeDomains(request).Should().Contain("AI");
    }

    [Fact]
    public void The_split_AI_table_still_recognises_both_halves()
    {
        // The 17 AI terms from the original single table were split 11 kernel / 6 game.
        // DomainRecognizer merges them under one key, so with the game layer installed the
        // behaviour must be indistinguishable from before the split. This is the assertion
        // that would catch the merge dropping one side.
        var sut = WithGameLayer();

        sut.RecognizeDomains("the agent uses a neural network").Should().Contain("AI");  // kernel half
        sut.RecognizeDomains("fix npc pathfinding").Should().Contain("AI");              // game half
    }

    [Fact]
    public void AddGameDomainPatterns_wires_the_provider_through_the_container()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<DomainRecognizer>();
        services.AddGameDomainPatterns();
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<DomainRecognizer>()
            .RecognizeDomains("balance the weapon damage")
            .Should().Contain("Combat");
    }
}
