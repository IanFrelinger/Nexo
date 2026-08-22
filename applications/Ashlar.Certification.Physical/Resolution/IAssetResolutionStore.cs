namespace Ashlar.Certification.Physical.Resolution;

/// <summary>
/// Resolves hosted digital-twin assets and registered certificates for physical atoms.
/// </summary>
public interface IAssetResolutionStore
{
    bool TryResolveAsset(string assetHash, string assetVersion, out DigitalTwinAssetRecord? asset);

    bool TryResolveCert(Guid atomId, out PhysicalAtomCertificate? certificate);
}
