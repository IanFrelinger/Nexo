using Nexo.Certification.Physical;
using Nexo.Certification.Physical.Resolution;

namespace Nexo.Infrastructure.Certification.Physical;

/// <summary>
/// Input to deterministic asset bundle certification and registration.
/// </summary>
public sealed class AssetBundleCertificationRequest
{
    public Guid AtomId { get; init; }

    public BindingScope BindingScope { get; init; }

    public required byte[] AssetBytes { get; init; }

    public required string AssetVersion { get; init; }

    public string ContentType { get; init; } = "application/octet-stream";

    public GeoAnchor? GeoAnchor { get; init; }

    public ManufactureMeta? ManufactureMeta { get; init; }

    public IReadOnlyDictionary<string, byte[]> Extensions { get; init; } =
        new Dictionary<string, byte[]>(StringComparer.Ordinal);
}

public sealed record AssetBundleCertificationResult(
    bool Succeeded,
    PhysicalAtomCertBundle? Bundle,
    string? FailureCode,
    string? Reason)
{
    public static AssetBundleCertificationResult Ok(PhysicalAtomCertBundle bundle) =>
        new(true, bundle, null, null);

    public static AssetBundleCertificationResult Refused(string code, string reason) =>
        new(false, null, code, reason);
}

/// <summary>
/// Phase 1 pipeline: issue certificate, register asset + cert in resolution store, emit portable bundle.
/// </summary>
public sealed class AssetBundleCertificationPipeline
{
    private readonly BundleCertificationBrick _issuer;

    public AssetBundleCertificationPipeline(BundleCertificationBrick issuer)
    {
        _issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
    }

    public AssetBundleCertificationResult CertifyAndRegister(
        InMemoryAssetResolutionStore store,
        AssetBundleCertificationRequest request,
        ReadOnlySpan<byte> issuerPublicKey)
    {
        if (store is null)
            return AssetBundleCertificationResult.Refused("store-missing", "Asset resolution store is required.");

        if (issuerPublicKey.IsEmpty)
            return AssetBundleCertificationResult.Refused("issuer-public-key-missing", "Issuer public key is required.");

        var issue = _issuer.Issue(new BundleCertificationRequest
        {
            AtomId = request.AtomId,
            BindingScope = request.BindingScope,
            AssetBytes = request.AssetBytes,
            AssetVersion = request.AssetVersion,
            GeoAnchor = request.GeoAnchor,
            ManufactureMeta = request.ManufactureMeta,
            Extensions = request.Extensions
        });

        if (!issue.Succeeded || issue.Certificate is null)
            return AssetBundleCertificationResult.Refused(issue.FailureCode!, issue.Reason!);

        var assetHash = AssetContentHasher.ComputeSha256Hex(request.AssetBytes);
        store.RegisterAsset(new DigitalTwinAssetRecord(
            assetHash,
            request.AssetVersion,
            request.ContentType,
            request.AssetBytes.ToArray()));

        store.RegisterCert(issue.Certificate);

        var bundle = new PhysicalAtomCertBundle(
            issue.Certificate,
            request.AssetBytes.ToArray(),
            request.ContentType,
            issuerPublicKey.ToArray());

        return AssetBundleCertificationResult.Ok(bundle);
    }
}
