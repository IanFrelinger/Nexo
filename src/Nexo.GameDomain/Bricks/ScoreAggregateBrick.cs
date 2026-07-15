using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.GameDomain.Bricks;

/// <summary>
/// Folds weighted score parts into a single score (deterministic aggregate brick).
/// </summary>
public sealed class ScoreAggregateBrick : DeterministicGameplayBrick
{
    /// <summary>Creates the score aggregate brick.</summary>
    public ScoreAggregateBrick()
    {
        Id = "score-aggregate";
        Name = "Score Aggregate";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "Computes totalScore = baseScore + (comboCount * comboWeight) + bonusScore.";
        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("baseScore", "int", "Base points"),
                new BrickInputDefinition("comboCount", "int", "Combo multiplier count"),
                new BrickInputDefinition("comboWeight", "int", "Points per combo step"),
                new BrickInputDefinition("bonusScore", "int", "Flat bonus", required: false, defaultValue: 0)
            ],
            Outputs =
            [
                new BrickOutputDefinition("totalScore", "int", "Aggregated score"),
                new BrickOutputDefinition("comboContribution", "int", "Points from combo")
            ]
        };
    }

    /// <inheritdoc />
    protected override BrickOutput ExecuteDeterministic(BrickInput input, IExecutionContext context)
    {
        var baseScore = input.Get<int>("baseScore");
        var comboCount = input.Get<int>("comboCount");
        var comboWeight = input.Get<int>("comboWeight");
        var bonusScore = input.Get("bonusScore", 0);

        var comboContribution = Math.Max(0, comboCount) * comboWeight;
        var totalScore = baseScore + comboContribution + bonusScore;

        var output = new BrickOutput { Summary = $"Total score: {totalScore}" };
        output.Set("totalScore", totalScore);
        output.Set("comboContribution", comboContribution);
        return output;
    }
}
