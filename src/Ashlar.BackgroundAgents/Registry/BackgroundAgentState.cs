using Ashlar.Abstractions;
using Ashlar.BackgroundAgents.Configuration;

namespace Ashlar.BackgroundAgents.Registry;

/// <summary>
/// Background agent state enumeration.
/// </summary>
public enum BackgroundAgentState
{
    /// <summary>
    /// Agent is idle (not running).
    /// </summary>
    Idle,

    /// <summary>
    /// Agent is starting.
    /// </summary>
    Starting,

    /// <summary>
    /// Agent is running.
    /// </summary>
    Running,

    /// <summary>
    /// Agent is stopping.
    /// </summary>
    Stopping,

    /// <summary>
    /// Agent is stopped.
    /// </summary>
    Stopped,

    /// <summary>
    /// Agent encountered an error.
    /// </summary>
    Error
}
