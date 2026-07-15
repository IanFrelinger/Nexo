namespace Nexo.GameDomain;

/// <summary>
/// Fan-in join policy for parallel gameplay brick candidates
/// (mirrors pipeline <c>PipelineJoinStrategy</c> without Infrastructure).
/// </summary>
public enum GameplayJoinStrategy
{
    /// <summary>All candidates must succeed.</summary>
    All = 0,

    /// <summary>Ready when at least one candidate succeeds.</summary>
    FirstSuccess = 1,

    /// <summary>Ready when <see cref="GameplayAggregateRequest.Quorum"/> successes are met.</summary>
    Quorum = 2
}
