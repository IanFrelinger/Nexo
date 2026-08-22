namespace Ashlar.Abstractions.Barriers;

/// <summary>
/// Request-scoped accessor for the active <see cref="BarrierContext"/>.
/// Initialized once per inbound request at the orchestration boundary.
/// </summary>
public interface IBarrierContextAccessor
{
    /// <summary>Barrier context established for the current request scope, if initialized.</summary>
    BarrierContext? Current { get; }

    /// <summary>
    /// Set once at the request boundary.
    /// Throws if called more than once.
    /// </summary>
    void Initialize(BarrierContext context);
}
