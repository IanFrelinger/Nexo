namespace Nexo.Spatial.Runtime.Ports;

/// <summary>
/// Resolves a physical marker payload to a certified atom identity for pose binding.
/// </summary>
public interface IPhysicalAtomResolver
{
    PhysicalAtomResolveResult Resolve(string markerPayload);
}
