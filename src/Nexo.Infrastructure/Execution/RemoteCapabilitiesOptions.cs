namespace Nexo.Infrastructure.Execution;

/// <summary>
/// Options controlling stale remote capability behavior.
/// </summary>
public sealed class RemoteCapabilitiesOptions
{
    /// <summary>
    /// How long a freshly fetched capability snapshot stays in fast in-memory cache before refresh.
    /// </summary>
    public TimeSpan CapabilityTtl { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum allowed age for stale capability reuse.
    /// Set to less than or equal to zero to disable stale-age enforcement.
    /// </summary>
    public TimeSpan MaxStaleAge { get; set; } = TimeSpan.FromMinutes(10);
}

