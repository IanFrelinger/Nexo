namespace Nexo.Core.Application.NodeCapabilityRuntime.Models;

/// <summary>
/// Network connectivity characteristics affecting remote pulls and escalation.
/// </summary>
public sealed record NetworkLatencyProfile
{
    /// <summary>Whether the active connection is Wi-Fi rather than cellular.</summary>
    public bool IsWifi { get; init; }

    /// <summary>Whether the connection is subject to metered data charges.</summary>
    public bool IsMetered { get; init; }

    /// <summary>Average round-trip latency to the upstream endpoint in milliseconds.</summary>
    public int AverageLatencyMs { get; init; }
}
