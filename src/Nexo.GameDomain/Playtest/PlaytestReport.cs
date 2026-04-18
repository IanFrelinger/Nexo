namespace Nexo.GameDomain.Playtest;

/// <summary>
/// Results from a completed playtest session. Contains balance findings,
/// map analysis, performance metrics, proposed fixes, and video references.
/// </summary>
public sealed record PlaytestReport
{
    public string SessionId { get; init; } = string.Empty;
    public string ScenarioName { get; init; } = string.Empty;
    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;
    public double DurationSeconds { get; init; }

    public IReadOnlyList<BalanceFinding> BalanceFindings { get; init; } = Array.Empty<BalanceFinding>();
    public IReadOnlyList<MapFinding> MapFindings { get; init; } = Array.Empty<MapFinding>();
    public PerformanceMetrics Performance { get; init; } = new();
    public IReadOnlyList<ProposedFix> ProposedFixes { get; init; } = Array.Empty<ProposedFix>();
    public IReadOnlyList<VideoClip> VideoClips { get; init; } = Array.Empty<VideoClip>();

    public int TotalIssues => BalanceFindings.Count(f => f.Severity != "ok") + MapFindings.Count(f => f.Severity != "ok");
}

public sealed record BalanceFinding
{
    public string ItemName { get; init; } = string.Empty;
    public string ItemType { get; init; } = "weapon"; // weapon, ability, game_rule
    public string Severity { get; init; } = "ok"; // ok, warning, critical
    public double KillRateMultiplier { get; init; } = 1.0;
    public double Threshold { get; init; } = 2.0;
    public string Summary { get; init; } = string.Empty;
}

public sealed record MapFinding
{
    public string AreaName { get; init; } = string.Empty;
    public string Severity { get; init; } = "ok"; // ok, warning, critical
    public double DeathPercentage { get; init; }
    public string FindingType { get; init; } = "choke_point"; // choke_point, dead_zone, spawn_camp, sightline_issue
    public string Summary { get; init; } = string.Empty;
}

public sealed record PerformanceMetrics
{
    public double AverageFps { get; init; }
    public double P99Fps { get; init; }
    public int DrawCalls { get; init; }
    public double MemoryMb { get; init; }
    public int ActiveParticles { get; init; }
}

public sealed record ProposedFix
{
    public int FixNumber { get; init; }
    public string TargetSystem { get; init; } = string.Empty; // "Weapons/RocketLauncher", "Map/EastCorridor"
    public string Description { get; init; } = string.Empty;
    public string IterateCommand { get; init; } = string.Empty;
    public string Severity { get; init; } = "warning";
}

public sealed record VideoClip
{
    public string FileName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double StartSeconds { get; init; }
    public double EndSeconds { get; init; }
    public long FileSizeBytes { get; init; }
}
