namespace Nexo.Agents.UniversalTester.Models;

/// <summary>
/// Result of validating an action's outcome.
/// </summary>
public record ValidationResult
{
    public bool Passed { get; init; }
    public required string Reasoning { get; init; }
    public IReadOnlyList<DetectedIssue> IssuesFound { get; init; } = Array.Empty<DetectedIssue>();
    public double Confidence { get; init; }
}

/// <summary>
/// Result of executing an action.
/// </summary>
public record ActionExecutionResult
{
    public bool Success { get; init; }
    public TimeSpan Duration { get; init; }
    public string? Error { get; init; }
    public byte[]? ScreenshotBefore { get; init; }
    public byte[]? ScreenshotAfter { get; init; }
    public string? ResultDescription { get; init; }
}
