namespace Nexo.BackgroundAgents;

/// <summary>
/// Base result for background agent runner operations (host-agnostic).
/// All runner result types share Success and Summary for consistent logging and agent use.
/// </summary>
public abstract record BackgroundAgentRunResult(bool Success, string Summary);
