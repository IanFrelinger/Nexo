using FluentAssertions;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.GameDomain;
using Nexo.GameDomain.Bricks;

namespace Nexo.Tests.GameDomain;

public sealed class ScoreAggregateBrickTests
{
    [Fact]
    public async Task FoldsWeightedParts()
    {
        var input = new BrickInput();
        input.Set("baseScore", 100);
        input.Set("comboCount", 3);
        input.Set("comboWeight", 10);
        input.Set("bonusScore", 5);

        var output = await new ScoreAggregateBrick().ExecuteAsync(
            input,
            ImplementationType.Deterministic,
            GameplayExecutionContext.ForPlayer("p1"));

        output.Get<int>("comboContribution").Should().Be(30);
        output.Get<int>("totalScore").Should().Be(135);
    }
}
