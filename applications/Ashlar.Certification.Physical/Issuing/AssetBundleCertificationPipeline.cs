using Ashlar.Certification.Physical;
using Ashlar.Certification.Physical.Resolution;

namespace Ashlar.Certification.Physical.Issuing;

/// <summary>
/// Phase 1 pipeline: issue certificate, register asset + cert in resolution store, emit portable bundle.
/// </summary>
public sealed class AssetBundleCertificationPipeline
{
    private readonly BundleCertificationBrick _issuer;

    /// <summary>Initializes a new asset bundle certification pipeline.</summary>
    public AssetBundleCertificationPipeline(BundleCertificationBrick issuer)
    {
        _issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
    }

    /// <summary>Certify and register.</summary>
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
