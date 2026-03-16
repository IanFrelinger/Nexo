namespace Nexo.Runtime.Barriers;

/// <summary>
/// Thrown when a request barrier exceeds the host-configured ceiling.
/// </summary>
public sealed class BarrierCeilingExceededException : Exception
{
    public BarrierCeilingExceededException(
        string requestedLevel,
        string hostCeiling,
        string correlationId)
        : base($"Barrier level '{requestedLevel}' exceeds host ceiling '{hostCeiling}'.")
    {
        RequestedLevel = requestedLevel;
        HostCeiling = hostCeiling;
        CorrelationId = correlationId;
    }

    public string RequestedLevel { get; }

    public string HostCeiling { get; }

    public string CorrelationId { get; }

    public string ErrorCode => "BARRIER_CEILING_EXCEEDED";
}
