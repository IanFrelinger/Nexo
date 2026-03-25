namespace Nexo.Orchestration.Barriers;

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

    public string AgentName { get; }

    public string CorrelationId { get; }

    public string CurrentLevel { get; }

    public string RequestedLevel { get; }

    public string ErrorCode => "BARRIER_ELEVATION_DENIED";
}
