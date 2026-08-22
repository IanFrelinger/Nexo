namespace Ashlar.Abstractions.Barriers;

/// <summary>
/// Ambient barrier context readable from singleton services (for example gRPC transport).
/// Updated when a scoped <see cref="IBarrierContextAccessor"/> is initialized for a request or CLI run.
/// </summary>
public interface IBarrierContextAmbient
{
    /// <summary>Barrier context currently propagated through ambient storage, if any.</summary>
    BarrierContext? Current { get; }

    /// <summary>
    /// Replaces the ambient barrier context for the current async flow.
    /// Called by scoped accessors when a request boundary is established or torn down.
    /// </summary>
    /// <param name="context">Barrier context to publish, or <see langword="null"/> to clear.</param>
    void SetCurrent(BarrierContext? context);
}
