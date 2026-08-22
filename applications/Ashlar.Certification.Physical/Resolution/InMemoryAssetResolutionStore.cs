namespace Ashlar.Certification.Physical.Resolution;

/// <summary>
/// In-memory resolution store for headless tests and local prototypes.
/// </summary>
public sealed class InMemoryAssetResolutionStore : IAssetResolutionStore
{
    private readonly Dictionary<(string Hash, string Version), DigitalTwinAssetRecord> _assets = new();
    private readonly Dictionary<Guid, PhysicalAtomCertificate> _certs = new();

    public void RegisterAsset(DigitalTwinAssetRecord asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        _assets[(asset.AssetHash, asset.AssetVersion)] = asset;
    }

    public void RegisterCert(PhysicalAtomCertificate certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        _certs[certificate.AtomId] = certificate;
    }

    public bool TryResolveAsset(string assetHash, string assetVersion, out DigitalTwinAssetRecord? asset)
    {
        if (_assets.TryGetValue((assetHash, assetVersion), out var record))
        {
            asset = record;
            return true;
        }

        asset = null;
        return false;
    }

    public bool TryResolveCert(Guid atomId, out PhysicalAtomCertificate? certificate)
    {
        if (_certs.TryGetValue(atomId, out var cert))
        {
            certificate = cert;
            return true;
        }

        certificate = null;
        return false;
    }
}
