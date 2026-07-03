namespace Nexo.Certification.Physical;

/// <summary>
/// Binds a physical atom to a hosted digital-twin asset via Ed25519 issuer attestation.
/// </summary>
public sealed record PhysicalAtomCertificate
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public MaturityLevel Maturity { get; init; } = MaturityLevel.Prototype;

    public Guid AtomId { get; init; }

    public BindingScope BindingScope { get; init; }

    /// <summary>Lowercase hex SHA-256 digest (64 characters) of the bound asset bytes.</summary>
    public string AssetHash { get; init; } = string.Empty;

    /// <summary>SemVer of the bound asset.</summary>
    public string AssetVersion { get; init; } = string.Empty;

    public GeoAnchor? GeoAnchor { get; init; }

    public ManufactureMeta? ManufactureMeta { get; init; }

    /// <summary>Signed but uninterpreted by the core verifier.</summary>
    public IReadOnlyDictionary<string, byte[]> Extensions { get; init; } =
        new Dictionary<string, byte[]>(StringComparer.Ordinal);

    /// <summary>Base64-encoded Ed25519 signature over the canonical signing payload.</summary>
    public string? IssuerSignature { get; init; }
}
