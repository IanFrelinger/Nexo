namespace Nexo.Abstractions.Barriers;

public static class BarrierAuditEventType
{
    public static readonly string ContextInitialized = "BARRIER_INITIALIZED";
    public static readonly string AgentInvoked = "BARRIER_AGENT_INVOKED";
    public static readonly string ElevationAttempted = "BARRIER_ELEVATION_ATTEMPTED";
    public static readonly string ElevationDenied = "BARRIER_ELEVATION_DENIED";
    public static readonly string PropagatedOverWire = "BARRIER_WIRE_PROPAGATED";
    public static readonly string ValidationFailed = "BARRIER_VALIDATION_FAILED";
    public static readonly string CeilingExceeded = "BARRIER_CEILING_EXCEEDED";
    public static readonly string DefaultApplied = "BARRIER_DEFAULT_APPLIED";
}
