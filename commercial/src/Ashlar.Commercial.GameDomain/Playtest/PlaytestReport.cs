namespace Ashlar.Commercial.GameDomain.Playtest;
/// <summary>
/// Results from a completed playtest session. Contains balance findings,
/// map analysis, performance metrics, proposed fixes, and video references.
/// </summary>
public sealed record PlaytestReport
{
    /// <summary>session id value.</summary>
    public string SessionId { get; init; } = string.Empty;
    /// <summary>Scenario name value.</summary>
    public string ScenarioName { get; init; } = string.Empty;
    /// <summary>Completed at value.</summary>
    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;
    /// <summary>Duration seconds value.</summary>
    public double DurationSeconds { get; init; }

    /// <summary>Balance findings value.</summary>
    public IReadOnlyList<BalanceFinding> BalanceFindings { get; init; } = Array.Empty<BalanceFinding>();
    /// <summary>Map findings value.</summary>
    public IReadOnlyList<MapFinding> MapFindings { get; init; } = Array.Empty<MapFinding>();
    /// <summary>Performance value.</summary>
    public PerformanceMetrics Performance { get; init; } = new();
    /// <summary>Proposed fixes value.</summary>
    public IReadOnlyList<ProposedFix> ProposedFixes { get; init; } = Array.Empty<ProposedFix>();
    /// <summary>Video clips value.</summary>
    public IReadOnlyList<VideoClip> VideoClips { get; init; } = Array.Empty<VideoClip>();

    /// <summary>Total issues value.</summary>
    public int TotalIssues => BalanceFindings.Count(f => f.Severity != "ok") + MapFindings.Count(f => f.Severity != "ok");
}
