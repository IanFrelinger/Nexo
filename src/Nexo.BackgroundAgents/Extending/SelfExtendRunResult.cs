namespace Nexo.BackgroundAgents.Extending;

/// <summary>
/// Host-agnostic result of a self-extend run (for background agent use).
/// </summary>
public record SelfExtendRunResult(bool Success, int ToolCallsExecuted, int ToolCallsDenied, string Summary) : BackgroundAgentRunResult(Success, Summary);
