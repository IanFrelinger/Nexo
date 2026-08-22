namespace Ashlar.Core.Application.NodeCapabilityRuntime.Models;

/// <summary>
/// Notification that node constraints have changed and may affect routing.
/// </summary>
public sealed record ConstraintUpdate
{
    /// <summary>Node profile snapshot before the change.</summary>
    public NodeProfile Previous { get; init; } = new();

    /// <summary>Node profile snapshot after the change.</summary>
    public NodeProfile Current { get; init; } = new();

    /// <summary>Human-readable reason for the constraint update.</summary>
    public string Reason { get; init; } = "ProfileUpdated";
}
