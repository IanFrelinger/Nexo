namespace Nexo.Abstractions.Barriers;

public interface IBarrierContextAccessor
{
    BarrierContext? Current { get; }

    /// <summary>
    /// Set once at the request boundary.
    /// Throws if called more than once.
    /// </summary>
    void Initialize(BarrierContext context);
}
