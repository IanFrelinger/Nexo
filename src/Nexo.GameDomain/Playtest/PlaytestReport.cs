namespace Nexo.GameDomain.Playtest;

/// <summary>
/// The result of a playtest session including issues found, video clips, and proposed fixes.
/// Stub: will be replaced by Phase 1 implementation.
/// </summary>
public sealed record PlaytestReport
{
    public string SessionId { get; init; } = string.Empty;
    public string ScenarioName { get; init; } = string.Empty;
    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;
    public int TotalIssues =>
        BalanceFindings.Count(f => f.Severity != "ok") +
        MapFindings.Count(f => f.Severity != "ok");
    public int CriticalIssues { get; init; }
    public double DurationSeconds { get; init; }
    public bool Passed { get; init; }
    public IReadOnlyList<PlaytestIssue> Issues { get; init; } = [];
    public IReadOnlyList<VideoClip> VideoClips { get; init; } = [];
    public IReadOnlyList<ProposedFix> ProposedFixes { get; init; } = [];
    public IReadOnlyList<BalanceFinding> BalanceFindings { get; init; } = [];
    public IReadOnlyList<MapFinding> MapFindings { get; init; } = [];
    public PerformanceMetrics Performance { get; init; } = new();
}

public sealed record PlaytestIssue
{
    public string Id { get; init; } = string.Empty;
    public string Severity { get; init; } = "Info";
    public string Description { get; init; } = string.Empty;
    public string? Component { get; init; }
}

public sealed record VideoClip
{
    public string FileName { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double StartSeconds { get; init; }
    public double EndSeconds { get; init; }
    public long FileSizeBytes { get; init; }
}

public sealed record ProposedFix
{
    public int FixNumber { get; init; }
    public string Description { get; init; } = string.Empty;
    public string TargetFile { get; init; } = string.Empty;
    public string TargetSystem { get; init; } = string.Empty;
    public string Severity { get; init; } = "warning";
    public string IterateCommand { get; init; } = string.Empty;
    public string? Diff { get; init; }
}

public sealed record BalanceFinding
{
    public string ItemName { get; init; } = string.Empty;
    public string Severity { get; init; } = "ok";
    public string Summary { get; init; } = string.Empty;
    public double KillRateMultiplier { get; init; }
}

public sealed record MapFinding
{
    public string AreaName { get; init; } = string.Empty;
    public string FindingType { get; init; } = string.Empty;
    public string Severity { get; init; } = "ok";
    public string Summary { get; init; } = string.Empty;
    public double DeathPercentage { get; init; }
}

public sealed record PerformanceMetrics
{
    public double AverageFps { get; init; }
    public double P99Fps { get; init; }
    public int DrawCalls { get; init; }
    public double MemoryMb { get; init; }
    public int ActiveParticles { get; init; }
}
