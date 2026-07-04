namespace Nexo.Abstractions.Barriers;

/// <summary>
/// Canonical barrier audit event type strings written to <see cref="IBarrierAuditLog"/>.
/// Values are stable wire identifiers consumed by audit sinks and operator dashboards.
/// </summary>
public static class BarrierAuditEventType
{
    /// <summary>Barrier context was initialized for an inbound request.</summary>
    public static readonly string ContextInitialized = "BARRIER_INITIALIZED";

    /// <summary>An agent was invoked under the current barrier context.</summary>
    public static readonly string AgentInvoked = "BARRIER_AGENT_INVOKED";

    /// <summary>A caller attempted to elevate the active barrier level.</summary>
    public static readonly string ElevationAttempted = "BARRIER_ELEVATION_ATTEMPTED";

    /// <summary>An elevation attempt was denied by policy or resolver rules.</summary>
    public static readonly string ElevationDenied = "BARRIER_ELEVATION_DENIED";

    /// <summary>Barrier context was propagated across a wire/transport boundary.</summary>
    public static readonly string PropagatedOverWire = "BARRIER_WIRE_PROPAGATED";

    /// <summary>Barrier validation failed (missing context, invalid level, etc.).</summary>
    public static readonly string ValidationFailed = "BARRIER_VALIDATION_FAILED";

    /// <summary>The resolved barrier level exceeded the configured ceiling.</summary>
    public static readonly string CeilingExceeded = "BARRIER_CEILING_EXCEEDED";

    /// <summary>A default barrier level was applied because none could be resolved.</summary>
    public static readonly string DefaultApplied = "BARRIER_DEFAULT_APPLIED";

    /// <summary>Identity material was successfully resolved to a barrier level.</summary>
    public static readonly string IdentityResolved = "BARRIER_IDENTITY_RESOLVED";
}
