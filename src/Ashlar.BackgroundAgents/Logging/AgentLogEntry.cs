namespace Ashlar.BackgroundAgents.Logging;

/// <summary>
/// A single log entry for a background agent execution.
/// </summary>
/// <param name="AgentId">Agent identifier.</param>
/// <param name="Timestamp">When the entry was created.</param>
/// <param name="Level">Log level (Debug, Info, Warning, Error).</param>
/// <param name="Message">Log message.</param>
public sealed record AgentLogEntry(string AgentId, DateTimeOffset Timestamp, string Level, string Message);
