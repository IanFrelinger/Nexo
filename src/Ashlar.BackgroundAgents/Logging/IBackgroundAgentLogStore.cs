namespace Ashlar.BackgroundAgents.Logging;

/// <summary>
/// Store for background agent execution logs (for CLI logs command and debugging).
/// </summary>
public interface IBackgroundAgentLogStore
{
    /// <summary>
    /// Append a log entry for an agent.
    /// </summary>
    void Append(string agentId, string level, string message);

    /// <summary>
    /// Get recent log entries for an agent.
    /// </summary>
    /// <param name="agentId">Agent ID.</param>
    /// <param name="maxCount">Maximum number of entries to return (e.g. tail N).</param>
    /// <param name="levelFilter">Optional level filter (Debug, Info, Warning, Error); null = all.</param>
    /// <param name="since">Optional minimum timestamp; null = no filter.</param>
    IReadOnlyList<AgentLogEntry> GetRecent(string agentId, int maxCount = 100, string? levelFilter = null, DateTimeOffset? since = null);
}
