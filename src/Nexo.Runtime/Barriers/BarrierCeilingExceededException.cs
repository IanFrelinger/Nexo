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

    /// <summary>Requested barrier level that exceeded the host ceiling.</summary>
    public string RequestedLevel { get; }

    /// <summary>Host-configured maximum allowed barrier level.</summary>
    public string HostCeiling { get; }

    /// <summary>Correlation identifier associated with the rejected request.</summary>
    public string CorrelationId { get; }

    /// <summary>Stable error code for barrier ceiling violations.</summary>
    public string ErrorCode => "BARRIER_CEILING_EXCEEDED";
}
