namespace Nexo.Agents.UniversalTester.Models;

/// <summary>
/// Complete test session state.
/// </summary>
public record TestSession
{
    public required string Goal { get; init; }
    public required string TargetDescription { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public IReadOnlyList<TestStep> Steps { get; init; } = Array.Empty<TestStep>();
    public IReadOnlyList<DetectedIssue> AllIssues { get; init; } = Array.Empty<DetectedIssue>();
    public int ProgressPercent { get; init; }
    public bool GoalAchieved { get; init; }
    public string? StopReason { get; init; }
}

/// <summary>
/// A single step in the test execution.
/// </summary>
public record TestStep
{
    public int StepNumber { get; init; }
    public required TestAction Action { get; init; }
    public required ActionExecutionResult ExecutionResult { get; init; }
    public required ValidationResult Validation { get; init; }
    public Understanding? UnderstandingBefore { get; init; }
}

/// <summary>
/// Test report output.
/// </summary>
public record TestReport
{
    public required TestSummary Summary { get; init; }
    public string? HtmlReport { get; init; }
    public string? JsonReport { get; init; }
}

/// <summary>
/// Summary of test results.
/// </summary>
public record TestSummary
{
    public int TotalTests { get; init; }
    public int Passed { get; init; }
    public int Failed { get; init; }
    public int Warnings { get; init; }
    public TimeSpan Duration { get; init; }
    public double OverallScore { get; init; }
    public IReadOnlyList<string> KeyFindings { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Recommendations { get; init; } = Array.Empty<string>();
}
