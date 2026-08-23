namespace Ashlar.Abstractions.Barriers;

/// <summary>
/// Operator-defined barrier configuration.
/// </summary>
public sealed class BarrierOptions
{
    /// <summary>
    /// Ordered list of barrier levels from lowest to highest sensitivity.
    /// </summary>
    public IList<string> Levels { get; init; } = [];

    /// <summary>
    /// When true, requests without an explicit barrier level are rejected.
    /// </summary>
    public bool RequireExplicitBarrier { get; init; }

    /// <summary>
    /// Highest barrier level this host is allowed to process.
    /// Null disables host-ceiling checks.
    /// </summary>
    public string? HostCeiling { get; init; }
}
