namespace Nexo.Feature.API.Enums;

/// <summary>
/// Represents the health status of a service
/// </summary>
public enum ServiceHealthStatus
{
    /// <summary>
    /// Service health status is unknown
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Service is healthy and responding
    /// </summary>
    Healthy = 1,

    /// <summary>
    /// Service is unhealthy or not responding
    /// </summary>
    Unhealthy = 2,

    /// <summary>
    /// Service is degraded but still functional
    /// </summary>
    Degraded = 3,

    /// <summary>
    /// Service is starting up
    /// </summary>
    Starting = 4,

    /// <summary>
    /// Service is shutting down
    /// </summary>
    Stopping = 5,

    /// <summary>
    /// Service is stopped
    /// </summary>
    Stopped = 6
} 