namespace Ashlar.Orchestration.Playtest.Models;

/// <summary>
/// Playtest report with balance analysis.
/// </summary>
public sealed record PlaytestReport
{
    /// <summary>Unique report identifier.</summary>
    public required string ReportId { get; init; }

    /// <summary>Playtest sessions included in the analysis.</summary>
    public IReadOnlyList<string> SessionIds { get; init; } = Array.Empty<string>();

    /// <summary>UTC timestamp when the report was generated.</summary>
    public DateTimeOffset GeneratedAt { get; init; }

    /// <summary>Balance issues identified across sessions.</summary>
    public IReadOnlyList<BalanceIssue> Issues { get; init; } = Array.Empty<BalanceIssue>();

    /// <summary>Optional executive summary of findings.</summary>
    public string? Summary { get; init; }
}
