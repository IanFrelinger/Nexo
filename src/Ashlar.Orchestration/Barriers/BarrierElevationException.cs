namespace Ashlar.Orchestration.Barriers;

/// <summary>Thrown when an agent invocation attempts to elevate above the current barrier level.</summary>
public sealed class BarrierElevationException : Exception
{
    public BarrierElevationException(
        string agentName,
        string correlationId,
        string currentLevel,
        string requestedLevel)
        : base($"Barrier elevation denied for agent '{agentName}': '{currentLevel}' -> '{requestedLevel}'.")
    {
        AgentName = agentName;
        CorrelationId = correlationId;
        CurrentLevel = currentLevel;
        RequestedLevel = requestedLevel;
    }

    /// <summary>Name of the agent that attempted elevation.</summary>
    public string AgentName { get; }

    /// <summary>Correlation identifier for the denied invocation.</summary>
    public string CorrelationId { get; }

    /// <summary>Current permitted barrier level.</summary>
    public string CurrentLevel { get; }

    /// <summary>Barrier level that was requested.</summary>
    public string RequestedLevel { get; }

    public string ErrorCode => "BARRIER_ELEVATION_DENIED";
}
