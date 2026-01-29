namespace Nexo.BackgroundAgents.Testing;

/// <summary>
/// Host-agnostic result of a single test run (for background agent use).
/// </summary>
public record TestRunResult(bool Success, int TotalTests, int PassedTests, int FailedTests, string Summary) : BackgroundAgentRunResult(Success, Summary);
