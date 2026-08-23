namespace Ashlar.Core.Application.NodeCapabilityRuntime.Models;

/// <summary>
/// Device thermal throttling state affecting inference eligibility.
/// </summary>
public enum ThermalState
{
    /// <summary>Temperature is within normal operating range.</summary>
    Nominal,

    /// <summary>Slight thermal pressure; performance may be modestly reduced.</summary>
    Fair,

    /// <summary>Significant thermal pressure; heavy workloads should be deferred.</summary>
    Serious,

    /// <summary>Critical thermal state; inference should be avoided.</summary>
    Critical,

    /// <summary>Severe thermal state; device may shut down background work.</summary>
    Severe
}
