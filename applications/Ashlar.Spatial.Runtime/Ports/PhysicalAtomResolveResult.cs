namespace Ashlar.Spatial.Runtime.Ports;

/// <summary>
/// Result of resolving a physical atom by marker payload.
/// </summary>
public sealed class PhysicalAtomResolveResult
{
    public PhysicalAtomResolveResult(bool found, ResolvedAtomIdentity? record)
    {
        Found = found;
        Record = record;
    }

    /// <summary>Whether a certified physical atom was resolved for the marker payload.</summary>
    public bool Found { get; }

    /// <summary>Resolved atom identity when <see cref="Found"/> is true.</summary>
    public ResolvedAtomIdentity? Record { get; }
}
