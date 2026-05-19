namespace Nexo.Abstractions.Barriers;

/// <summary>
/// Ambient barrier context readable from singleton services (for example gRPC transport).
/// Updated when a scoped <see cref="IBarrierContextAccessor"/> is initialized for a request or CLI run.
/// </summary>
public interface IBarrierContextAmbient
{
    BarrierContext? Current { get; }

    void SetCurrent(BarrierContext? context);
}
