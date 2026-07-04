namespace Nexo.BackgroundAgents.Optimization;

/// <summary>
/// Result of a single code analysis run (host-agnostic summary).
/// </summary>
public record CodeAnalysisRunResult(bool Success, int ViolationCount, string Summary) : BackgroundAgentRunResult(Success, Summary);
