namespace Nexo.Spatial.Runtime.Ports;

/// <summary>
/// Resolves a physical marker payload to a certified atom identity for pose binding.
/// </summary>
public interface IPhysicalAtomResolver
{
    PhysicalAtomResolveResult Resolve(string markerPayload);
}

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

    public bool Found { get; }

    public ResolvedAtomIdentity? Record { get; }
}
