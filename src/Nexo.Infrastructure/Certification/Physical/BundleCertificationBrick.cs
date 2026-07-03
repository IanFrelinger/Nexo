using Nexo.Certification.Physical;

namespace Nexo.Infrastructure.Certification.Physical;

/// <summary>
/// Input to deterministic physical-atom certificate issuance.
/// </summary>
public sealed class BundleCertificationRequest
{
    public Guid AtomId { get; init; }

    public BindingScope BindingScope { get; init; }

    public required byte[] AssetBytes { get; init; }

    public required string AssetVersion { get; init; }

    public GeoAnchor? GeoAnchor { get; init; }

    public ManufactureMeta? ManufactureMeta { get; init; }

    public IReadOnlyDictionary<string, byte[]> Extensions { get; init; } =
        new Dictionary<string, byte[]>(StringComparer.Ordinal);
}

public sealed record BundleCertificationResult(
    bool Succeeded,
    PhysicalAtomCertificate? Certificate,
    string? FailureCode,
    string? Reason)
{
    public static BundleCertificationResult Ok(PhysicalAtomCertificate certificate) =>
        new(true, certificate, null, null);

    public static BundleCertificationResult Refused(string code, string reason) =>
        new(false, null, code, reason);
}

/// <summary>
/// Deterministic certification brick: issues signed <see cref="PhysicalAtomCertificate"/> values
/// using the cert-gate Ed25519 signing pattern. Refuses inconsistent binding_scope inputs.
/// </summary>
public sealed class BundleCertificationBrick
{
    private readonly byte[] _issuerPrivateKey;

    public BundleCertificationBrick(ReadOnlySpan<byte> issuerPrivateKey)
    {
        if (issuerPrivateKey.IsEmpty)
            throw new ArgumentException("Issuer private key is required.", nameof(issuerPrivateKey));

        _issuerPrivateKey = issuerPrivateKey.ToArray();
    }

    public BundleCertificationResult Issue(BundleCertificationRequest request)
    {
        if (request is null)
            return BundleCertificationResult.Refused("request-missing", "Certification request is required.");

        if (request.AssetBytes is null || request.AssetBytes.Length == 0)
            return BundleCertificationResult.Refused("asset-bytes-missing", "Asset bytes are required.");

        var assetHash = AssetContentHasher.ComputeSha256Hex(request.AssetBytes);
        var unsigned = new PhysicalAtomCertificate
        {
            AtomId = request.AtomId,
            BindingScope = request.BindingScope,
            AssetHash = assetHash,
            AssetVersion = request.AssetVersion,
            GeoAnchor = request.GeoAnchor,
            ManufactureMeta = request.ManufactureMeta,
            Extensions = request.Extensions
        };

        if (!PhysicalAtomCertificateValidationPolicy.ValidateBindingScopeConsistency(
                unsigned,
                out var failureCode,
                out var reason))
        {
            return BundleCertificationResult.Refused(failureCode!, reason!);
        }

        var signed = unsigned with
        {
            IssuerSignature = PhysicalAtomCertificateSigning.Sign(unsigned, _issuerPrivateKey)
        };

        return BundleCertificationResult.Ok(signed);
    }
}
