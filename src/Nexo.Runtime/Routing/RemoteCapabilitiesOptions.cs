namespace Nexo.Runtime.Routing;

/// <summary>
/// Runtime-layer options controlling remote capability cache freshness and stale fallback.
/// </summary>
internal sealed class RemoteCapabilitiesOptions
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
