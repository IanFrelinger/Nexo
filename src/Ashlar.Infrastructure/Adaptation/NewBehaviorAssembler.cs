using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Core.Domain.Behaviors;
using Ashlar.Core.Domain.Bricks;

namespace Ashlar.Infrastructure.Adaptation;

/// <summary>
/// Assembles new behaviors from bricks using goal-driven selection.
/// Ranks bricks by keyword relevance to the goal and builds a dependency-ordered pipeline.
/// </summary>
public sealed class NewBehaviorAssembler : INewBehaviorAssembler
{
    private static readonly Dictionary<string, string[]> GoalKeywordToBrickIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["analyze"] = ["code-analysis", "validation", "observation.context"],
        ["security"] = ["owasp-scanner", "code-analysis", "validation"],
        ["validate"] = ["validation", "observation.context"],
        ["fix"] = ["observation.context", "validation"],
        ["adapt"] = ["observation.context"],
        ["observe"] = ["observation.context"],
        ["test"] = ["validation", "observation.context"],
        ["report"] = ["validation", "reporting"],
    };

    /// <inheritdoc />
    public Task<Behavior> AssembleAsync(
        string goal,
        IReadOnlyList<string> availableBrickIds,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var ranked = RankBricksByGoal(goal, availableBrickIds);
        var steps = new List<BehaviorStep>();
        for (var i = 0; i < ranked.Count; i++)
        {
            var brickId = ranked[i];
            var prevOutput = i == 0 ? "input" : $"step_{i}";
            steps.Add(new BehaviorStep
            {
                Id = $"step_{i + 1}",
                BrickId = brickId,
                Implementation = ImplementationType.Auto,
                InputMapping = new Dictionary<string, string> { ["input"] = prevOutput },
                OutputMapping = new Dictionary<string, string> { ["result"] = $"step_{i + 1}" },
            });
        }

        if (steps.Count == 0 && availableBrickIds.Count > 0)
        {
            foreach (var bid in availableBrickIds.Take(3))
            {
                steps.Add(new BehaviorStep
                {
                    Id = $"step_{steps.Count + 1}",
                    BrickId = bid,
                    Implementation = ImplementationType.Auto,
                    InputMapping = new Dictionary<string, string>(),
                    OutputMapping = new Dictionary<string, string>(),
                });
            }
        }

        var behavior = new Behavior
        {
            Id = $"assembled.{Guid.NewGuid():N}",
            Name = $"Assembled: {goal[..Math.Min(60, goal.Length)]}",
            Version = "1.0.0",
            Description = goal,
            Steps = steps,
            Inputs = [],
            Outputs = [],
        };

        return Task.FromResult(behavior);
    }

    private static List<string> RankBricksByGoal(string goal, IReadOnlyList<string> availableBrickIds)
    {
        var goalLower = goal.ToLowerInvariant();
        var scored = new List<(string Id, int Score)>();

        foreach (var bid in availableBrickIds)
        {
            var score = 0;
            foreach (var (keyword, preferred) in GoalKeywordToBrickIds)
            {
                if (!goalLower.Contains(keyword, StringComparison.Ordinal)) continue;
                var idx = Array.FindIndex(preferred, p => string.Equals(p, bid, StringComparison.OrdinalIgnoreCase));
                score += idx >= 0 ? (10 - idx) : 0;
            }
            if (score > 0)
                scored.Add((bid, score));
        }

        return scored
            .OrderByDescending(x => x.Score)
            .Select(x => x.Id)
            .Distinct()
            .Take(5)
            .ToList();
    }
}
