using FluentAssertions;
using GameDirector.Bricks;
using Nexo.Infrastructure.Execution;
using Xunit;

namespace Nexo.Tests.GameDirector;

[Trait("Category", "GameDirectorApplication")]
public sealed class BrickRegistryTests
{
    [Fact]
    public void Registry_resolves_all_game_director_bricks()
    {
        var registry = GameDirectorTestHost.CreateBrickRegistry();

        registry.GetBrick("balance.analysis").Should().BeOfType<BalanceAnalysisBrick>();
        registry.GetBrick("map.flow").Should().BeOfType<MapFlowBrick>();
        registry.GetBrick("content.generation").Should().BeOfType<ContentGenerationBrick>();
        registry.GetAllBricks().Should().HaveCount(3);
    }

    [Fact]
    public void GetBrick_returns_null_for_unknown_id()
    {
        var registry = GameDirectorTestHost.CreateBrickRegistry();
        registry.GetBrick("unknown").Should().BeNull();
    }
}
