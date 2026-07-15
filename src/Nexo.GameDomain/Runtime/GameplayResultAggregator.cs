using Nexo.Core.Domain.Execution;

namespace Nexo.GameDomain;

/// <summary>One parallel gameplay candidate result.</summary>
public sealed class GameplayCandidateResult
{
    /// <summary>Creates a candidate result.</summary>
    public GameplayCandidateResult(
        string brickId,
        bool succeeded,
        BrickOutput? output = null,
        TimeSpan duration = default,
        string? error = null)
    {
        BrickId = brickId ?? throw new ArgumentNullException(nameof(brickId));
        Succeeded = succeeded;
        Output = output;
        Duration = duration;
        Error = error;
    }

    /// <summary>Brick id that produced this candidate.</summary>
    public string BrickId { get; }

    /// <summary>Whether execution succeeded.</summary>
    public bool Succeeded { get; }

    /// <summary>Output when <see cref="Succeeded"/> is true.</summary>
    public BrickOutput? Output { get; }

    /// <summary>Wall time for this candidate.</summary>
    public TimeSpan Duration { get; }

    /// <summary>Error detail when failed.</summary>
    public string? Error { get; }
}

/// <summary>Input to <see cref="GameplayResultAggregator"/>.</summary>
public sealed class GameplayAggregateRequest
{
    /// <summary>Creates an aggregate request.</summary>
    public GameplayAggregateRequest(
        IReadOnlyList<GameplayCandidateResult> candidates,
        GameplayJoinStrategy joinStrategy = GameplayJoinStrategy.FirstSuccess,
        int quorum = 1)
    {
        Candidates = candidates ?? throw new ArgumentNullException(nameof(candidates));
        JoinStrategy = joinStrategy;
        Quorum = quorum < 1 ? 1 : quorum;
    }

    /// <summary>Candidate results to fold.</summary>
    public IReadOnlyList<GameplayCandidateResult> Candidates { get; }

    /// <summary>Fan-in policy.</summary>
    public GameplayJoinStrategy JoinStrategy { get; }

    /// <summary>Required successes when <see cref="GameplayJoinStrategy.Quorum"/>.</summary>
    public int Quorum { get; }
}

/// <summary>Aggregated gameplay candidates.</summary>
public sealed class GameplayAggregatedResult
{
    /// <summary>Creates an aggregated result.</summary>
    public GameplayAggregatedResult(
        bool joined,
        GameplayCandidateResult? best,
        IReadOnlyList<GameplayCandidateResult> candidates,
        int successCount)
    {
        Joined = joined;
        Best = best;
        Candidates = candidates ?? throw new ArgumentNullException(nameof(candidates));
        SuccessCount = successCount;
    }

    /// <summary>Whether the join strategy is satisfied.</summary>
    public bool Joined { get; }

    /// <summary>Preferred candidate (fastest success, else fewest errors).</summary>
    public GameplayCandidateResult? Best { get; }

    /// <summary>All candidates.</summary>
    public IReadOnlyList<GameplayCandidateResult> Candidates { get; }

    /// <summary>Count of successful candidates.</summary>
    public int SuccessCount { get; }
}

/// <summary>
/// Deterministic aggregator for parallel gameplay brick runs
/// (same spirit as adaptation <c>InstanceResultAggregator</c>).
/// </summary>
public static class GameplayResultAggregator
{
    /// <summary>Aggregates candidates under the requested join strategy.</summary>
    public static GameplayAggregatedResult Aggregate(GameplayAggregateRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var candidates = request.Candidates;
        var successes = candidates.Where(c => c.Succeeded).ToList();
        var successCount = successes.Count;

        var joined = request.JoinStrategy switch
        {
            GameplayJoinStrategy.All => successCount == candidates.Count && candidates.Count > 0,
            GameplayJoinStrategy.FirstSuccess => successCount >= 1,
            GameplayJoinStrategy.Quorum => successCount >= request.Quorum,
            _ => false
        };

        var best = SelectBest(candidates);
        return new GameplayAggregatedResult(joined, best, candidates, successCount);
    }

    private static GameplayCandidateResult? SelectBest(IReadOnlyList<GameplayCandidateResult> candidates)
    {
        if (candidates.Count == 0)
            return null;

        var passed = candidates.Where(c => c.Succeeded).ToList();
        if (passed.Count > 0)
            return passed.OrderBy(c => c.Duration).First();

        return candidates.OrderBy(c => c.Error?.Length ?? int.MaxValue).First();
    }
}
